using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointLogApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;


        public PointLogApiController(incodedatabaseContext context, IPointLogService pointLogService)
        {
            _context = context;
            _pointLogService = pointLogService;
        }

        /// 模擬領取任務獎勵 (加點)
        /// POST: /api/point/add-reward
        [HttpPost("add-reward")]
        [Authorize]
        public async Task<IActionResult> AddReward([FromForm] int changeAmount, [FromForm] int sourceId)
        {
            int UserId = User.GetId();

            if (changeAmount <= 0)
                return BadRequest("點數必須大於 0");

            var success = await _pointLogService.ChangePointsAsync(
                userId: UserId,
                changeAmount: changeAmount,
                sourceType: "entrust_mission",
                sourceId: sourceId,
                type: PointType.IncomeEntrustMission // 使用 Enum
            );

            if (!success) return BadRequest("獎勵領取失敗");

            return Ok("獎勵已發放");
        }

        //GET: api/PointLogApi
        [HttpGet]
        public async Task<IEnumerable<PointLogDTO>> GetPointLog()
        {
            return await _context.PointLog
        .AsNoTracking() // 建議查詢列表時加上，效能較好
        .Select(pl => new PointLogDTO
        {
            PointLogId = pl.PointLogId,
            UserId = pl.UserId,
            SourceType = pl.SourceType,
            SourceId = pl.SourceId,
            PointTypeDefinitionId = pl.PointTypeDefinitionId,
            ChangeAmount = pl.ChangeAmount,           
            BalanceAfter = pl.BalanceAfter,            
            Status = pl.Status,
            CreatedAt = pl.CreatedAt,
            DisplayName = pl.PointTypeDefinition.TypeName
        })
        .ToListAsync(); // 記得補上非同步執行
        }

        [HttpGet("user/my-logs")]
        [Authorize]
        public async Task<IEnumerable<PointLogDTO>> GetPointLogByUser()
        {
            int UserId = User.GetId(); 
            return await _pointLogService.GetUserLogsAsync(UserId);
        }
    }
}
