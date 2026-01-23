using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointLogApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public PointLogApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/PointLogApi
        [HttpGet]
        public async Task<IEnumerable<PointLogDTO>> GetPointLog()
        {
            return _context.PointLog.Select(pl=>new PointLogDTO
            {
                PointLogId=pl.PointLogId,
                UserId=pl.UserId,
                SourceType=pl.SourceType,
                SourceId=pl.SourceId,
                PointTypeDefinitionId=pl.PointTypeDefinitionId,
                ChangeAmount =pl.ChangeAmount,
                Status=pl.Status,
                CreatedAt =pl.CreatedAt
            });
        }

        //GET: api/PointLogApi/5
        //[HttpGet("{id}")]
        //public async Task<PointLogDTO> GetPointLog(int id)
        //{
        //    PointLogDTO? PlDTO = await _context.PointLog.Select(pl => new PointLogDTO
        //    {
        //        PointLogId = pl.PointLogId,
        //        UserId = pl.UserId,
        //        SourceType = pl.SourceType,
        //        SourceId = pl.SourceId,
        //        PointTypeDefinitionId = pl.PointTypeDefinitionId,
        //        ChangeAmount = pl.ChangeAmount,
        //        BalanceAfter = pl.BalanceAfter,
        //        CreatedAt = pl.CreatedAt
        //    }).FirstOrDefaultAsync(pl => pl.PointLogId == id);

        //    return PlDTO;
        //}

        // 修改後的 GET：只抓特定使用者的紀錄
        [HttpGet("user/{userId}")]
        public async Task<IEnumerable<PointLogDTO>> GetPointLogByUser(int userId)
        {
            return await _context.PointLog
                .Where(pl => pl.UserId == userId) // 關鍵：過濾使用者
                .OrderByDescending(pl => pl.CreatedAt) // 讓最新的在上面
                .Select(pl => new PointLogDTO
                {
                    PointLogId = pl.PointLogId,
                    UserId = pl.UserId,
                    SourceType = pl.SourceType,
                    SourceId = pl.SourceId,
                    PointTypeDefinitionId = pl.PointTypeDefinitionId,
                    ChangeAmount = pl.ChangeAmount,
                    Status = pl.Status,
                    CreatedAt = pl.CreatedAt
                })
                .ToListAsync();
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
