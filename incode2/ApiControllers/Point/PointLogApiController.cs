using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Dtos.Point;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        /// <summary>
        /// 模擬領取任務獎勵 (加點)
        /// POST: /api/point/add-reward
        /// </summary>
        [HttpPost("add-reward")]
        [Authorize]
        public async Task<IActionResult> AddReward([FromForm] int userId, [FromForm] int changeAmount, [FromForm] int sourceId)
        {
            // 直接從登入資訊拿 ID，不從參數拿，更安全
            int currentLoginUserId = User.GetId();

            // 檢查點數是否為 0，避免無謂的寫入
            //if (changeAmount <= 0) return BadRequest("點數必須大於 0");

            // 調用你的 Service
            var success = await _pointLogService.ChangePointsAsync(
                userId = currentLoginUserId,
                changeAmount,
                "entrust_mission",
                sourceId,
                PointType.EntrustMission // 使用 Enum
            );

            if (!success) return BadRequest("獎勵領取失敗");
            return Ok("獎勵已發放");
        }

        /// <summary>
        /// 模擬商城購買 (扣點)
        /// POST: /api/point/shop-purchase
        /// </summary>
        //[HttpPost("shop-purchase")]
        //public async Task<IActionResult> Purchase([FromForm] int userId, [FromForm] int price, [FromForm] int productId)
        //{
        //    int currentLoginUserId = User.GetId();
        //    if (price >= 0) return BadRequest("點數必須小於 0");

        //    var success = await _pointLogService.DeductPointsAsync(
        //        userId = currentLoginUserId,
        //        price,
        //        "orders",
        //        productId,
        //        PointType.Order // 使用 Enum
        //    );

        //    if (!success) return BadRequest("點數不足或購買失敗");
        //    return Ok("扣點成功，請繼續處理訂單");
        //}
    

        //GET: api/PointLogApi
       [HttpGet]
        public async Task<IEnumerable<PointLogDTO>> GetPointLog()
        {
            return _context.PointLog.Select(pl => new PointLogDTO
            {
                PointLogId = pl.PointLogId,
                UserId = pl.UserId,
                SourceType = pl.SourceType,
                SourceId = pl.SourceId,
                PointTypeDefinitionId = pl.PointTypeDefinitionId,
                ChangeAmount = pl.ChangeAmount,
                Status = pl.Status,
                CreatedAt = pl.CreatedAt
            });
        }

        [HttpGet("user/my-logs")]
        [Authorize]
        public async Task<IEnumerable<PointLogDTO>> GetPointLogByUser()
        {
            int UserId = User.GetId(); 
            return await _pointLogService.GetUserLogsAsync(UserId);
        }

        // PUT: api/PointLogApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ResultDTO> PutPointLog(int id, PointLogDTO PlDTO)
        {
            if (id != PlDTO.PointLogId)
            {
                return new ResultDTO { Ok=false, Code=400};
            }

            PointLog Pl = await _context.PointLog.FindAsync(id);
            if (Pl != null)
            {
                Pl.UserId = PlDTO.UserId;
                Pl.SourceType = PlDTO.SourceType;
                Pl.SourceId = PlDTO.SourceId;
                Pl.PointTypeDefinitionId = PlDTO.PointTypeDefinitionId;
                Pl.ChangeAmount = PlDTO.ChangeAmount;
                Pl.Status = PlDTO.Status;
                Pl.CreatedAt = PlDTO.CreatedAt;

                _context.Entry(Pl).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) //你要改的紀錄被別人先修改過了.所以你要修改就會顯示錯誤
                {
                    return new ResultDTO { Ok = false, Code = 500 }; //Server端的錯誤
                }

                return new ResultDTO { Ok = true, Code = 204 };
            }
            else
            {
                return new ResultDTO { Ok = false, Code = 404 };
            }
        }

        // POST: api/PointLogApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostPointLog(PointLogDTO PlDTO)
        {
            //entity 
            PointLog Pl = new PointLog
            {
                PointLogId=PlDTO.PointLogId, //id=0
                UserId =PlDTO.UserId,
                SourceType=PlDTO.SourceType,
                SourceId=PlDTO.SourceId,
                PointTypeDefinitionId=PlDTO.PointTypeDefinitionId,
                ChangeAmount =PlDTO.ChangeAmount,
                Status=PlDTO.Status,
                CreatedAt =PlDTO.CreatedAt,
            };
            _context.PointLog.Add(Pl);
            await _context.SaveChangesAsync(); //id+=? 你填的0會被覆蓋
            //因為會顯示PlDTO.PointLogId = -1 所以要補回Pl.PointLogId(entity)
            //PlDTO.PointLogId = Pl.PointLogId; //id+=? return PlDTO
            return new ResultDTO
            {
                Ok=true,
                Code=204 //NoContent
            };
        }

        // DELETE: api/PointLogApi/5
        [HttpDelete("{id}")]
        public async Task<ResultDTO> DeletePointLog(int id)
        {
            var pointLog = await _context.PointLog.FindAsync(id);
            if (pointLog == null)
            {
                return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
            }
            try
            {
                _context.PointLog.Remove(pointLog);
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException ex){ 
                return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示500
            }
            return new ResultDTO { Ok=true, Code=204};
        }
    }
}
