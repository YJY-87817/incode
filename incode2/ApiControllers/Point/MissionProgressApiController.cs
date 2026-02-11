using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Point;
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
    public class MissionProgressApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IMissionProgressService _missionProgressService;

        public MissionProgressApiController(incodedatabaseContext context, IMissionProgressService missionProgressService)
        {
            _context = context;
            _missionProgressService = missionProgressService;
        }

        //我的任務
        [HttpGet("my-missions")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserMissionViewDTO>>> GetMyMissions()
        {
            int UserId = User.GetId();

            if (UserId <= 0)
            {
                return Unauthorized(new ResultDTO
                {
                    Ok = false,
                    Code = 401,
                    Message = "使用者身份無效"
                });
            }

            var missions = await _missionProgressService.GetUserMissionsAsync(UserId);

            return Ok(missions);
        }

        //進度更新
        [HttpPost("{missionId}/increment")]
        [Authorize]
        public async Task<ActionResult<ResultDTO>> IncrementProgress(int missionId, [FromBody] IncrementRequest request = null)
        {
            int UserId = User.GetId();
            int increment = request?.Increment ?? 1;

            var result = await _missionProgressService.IncrementProgressAsync(UserId, missionId, increment);
            if (!result.Ok)
            {
                return result.Code switch
                {
                    400 => BadRequest(result),
                    404 => NotFound(result),
                    _ => StatusCode(500, result)
                };
            }
            return Ok(result);
        }

        public class IncrementRequest
        {
            public int Increment { get; set; } = 1;
        }


        [HttpPost("claim-reward")]
        [Authorize]
        public async Task<ActionResult<ResultDTO>> ClaimReward([FromBody] ClaimRewardRequest request)
        {
            int UserId = User.GetId();
            if (UserId <= 0)
            {
                return Unauthorized(new ResultDTO { Ok = false, Code = 401, Message = "使用者 ID 無效" });
            }

            int missionId = request.MissionId;

            // 先找到該使用者的該任務進度
            var progress = await _context.MissionProgress
                .FirstOrDefaultAsync(p => p.UserId == UserId && p.MissionId == missionId);

            if (progress == null)
                return NotFound(new ResultDTO { Ok = false, Code = 404, Message = "尚未領取此任務" });

            // 再用 missionId 呼叫服務（保持服務層不變）
            var result = await _missionProgressService.ClaimRewardAsync(UserId, missionId);

            if (!result.Ok)
            {
                return result.Code switch
                {
                    400 => BadRequest(result),
                    404 => NotFound(result),
                    _ => StatusCode(500, result)
                };
            }

            return Ok(result);
        }

        public class ClaimRewardRequest
        {
            public int MissionId { get; set; }
        }

        // GET: api/MissionProgressApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MissionProgressDTO>>> GetMissionProgress()
        {
            var list = await _missionProgressService.GetAllProgressAsync();
            return Ok(list);
        }

        // GET: api/MissionProgressApi/5
        [HttpGet("{id}")]
        public async Task<MissionProgressDTO> GetMissionProgress(int id)
        {
            MissionProgressDTO? MgDTO = await _context.MissionProgress.Select(mg => new MissionProgressDTO
            {
                MissionProgressId = mg.MissionProgressId,
                UserId = mg.UserId,
                MissionId = mg.MissionId,
                CurrentCount = mg.CurrentCount,
                Status = mg.Status,
                AcceptedAt = mg.AcceptedAt,
                CompletedAt = mg.CompletedAt,
                RewardedAt = mg.RewardedAt
            }).FirstOrDefaultAsync(mg=> mg.MissionProgressId == id);

            return MgDTO;
        }

        // PUT: api/MissionProgressApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ResultDTO> PutMissionProgress(int id, MissionProgress MgDTO)
        {
            if (id != MgDTO.MissionProgressId)
            {
                return new ResultDTO { Ok = false, Code = 400 };
            }

            MissionProgress Mg = await _context.MissionProgress.FindAsync(id);
            if (Mg != null)
            {
                Mg.MissionProgressId = MgDTO.MissionProgressId;
                Mg.UserId = MgDTO.UserId;
                Mg.MissionId = MgDTO.MissionId;
                Mg.CurrentCount = MgDTO.CurrentCount;
                Mg.Status = MgDTO.Status;
                // 2. 保護 AcceptedAt：如果前端沒傳，就保留資料庫原有的值
                Mg.AcceptedAt = MgDTO.AcceptedAt ?? Mg.AcceptedAt;
                // 3. 智慧型時間處理：針對狀態自動補時
                // 如果改為 "completed" 且資料庫還沒存過完工時間
                if (MgDTO.Status == "completed" && Mg.CompletedAt == null)
                {
                    Mg.CompletedAt = DateTime.Now;
                }
                else
                {
                    // 否則，依照前端傳入的值（可能是 null 或特定時間）更新
                    Mg.CompletedAt = MgDTO.CompletedAt ?? Mg.CompletedAt;
                }

                // 4. 領獎時間邏輯同上
                if (MgDTO.Status == "rewarded" && Mg.RewardedAt == null)
                {
                    Mg.RewardedAt = DateTime.Now;
                }
                else
                {
                    Mg.RewardedAt = MgDTO.RewardedAt ?? Mg.RewardedAt;
                }


                _context.Entry(Mg).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
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

        // POST: api/MissionProgressApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostMissionProgress(MissionProgressDTO MgDTO)
        {
            MissionProgress Mg = new MissionProgress 
            {
                MissionProgressId = MgDTO.MissionProgressId,
                UserId = MgDTO.UserId,
                MissionId = MgDTO.MissionId,
                CurrentCount = MgDTO.CurrentCount,
                Status = MgDTO.Status,
                AcceptedAt = DateTime.Now
            };

            _context.MissionProgress.Add(Mg);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 204 //NoContent
            };
        }

        // DELETE: api/MissionProgressApi/5
        [HttpDelete("{id}")]
        public async Task<ResultDTO> DeleteMissionProgress(int id)
        {
            var missionProgress = await _context.MissionProgress.FindAsync(id);
            if (missionProgress == null)
            {
                return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
            }
            try
            {
                _context.MissionProgress.Remove(missionProgress);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示500
            }

            return new ResultDTO { Ok = true, Code = 204 };
        }

        //private bool MissionProgressExists(int id)
        //{
        //    return _context.MissionProgress.Any(e => e.MissionProgressId == id);
        //}
    }
}
