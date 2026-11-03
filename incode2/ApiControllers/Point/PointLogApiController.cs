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
            int userId = User.GetId();

            if (changeAmount <= 0)
                return BadRequest("點數必須大於 0");

            // 1. 呼叫 Service 準備資料 (此時尚未進資料庫)
            var success = await _pointLogService.ChangePointsAsync(
                userId: userId,
                changeAmount: changeAmount,
                sourceType: "entrust_mission",
                sourceId: sourceId,
                type: PointType.IncomeEntrustMission
            );

            if (!success) return BadRequest("獎勵領取失敗");

            // 2. 【關鍵修正】執行實體保存，將 Log 寫入資料庫
            try
            {
                await _context.SaveChangesAsync();
                return Ok("獎勵已發放");
            }
            catch (DbUpdateException ex)
            {
                // 這裡可以記錄錯誤日誌
                return StatusCode(500, "資料庫寫入失敗");
            }
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
            int userId = User.GetId();
            return await _pointLogService.GetUserLogsAsync(userId);
        }
    }
}
