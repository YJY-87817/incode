using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Partials;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissionApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IMissionService _missionService;

        public MissionApiController(incodedatabaseContext context , IMissionService missionService)
        {
            _context = context;
            _missionService = missionService;
        }

        // GET: api/MissionApi
        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<MissionDTO>>> GetMission()
        {
            var mission = await _missionService.GetAllMissionAsync();
            return Ok(mission);
        }

        [HttpPost("{missionId}/missionClaim")]
        [Authorize]
        public async Task<ActionResult<ResultDTO>> ClaimMission(int missionId)
        {
            int userId = User.GetId();

            // 使用者驗證
            if (userId <= 0)
            {
                return Unauthorized(new ResultDTO
                {
                    Ok = false,
                    Code = 401,
                    Message = "使用者 ID 無效"
                });
            }

            // 任務 ID 驗證
            if (missionId <= 0)
            {
                return BadRequest(new ResultDTO
                {
                    Ok = false,
                    Code = 400,
                    Message = "任務 ID 無效"
                });
            }

            var result = await _missionService.ClaimMissionAsync(missionId, userId);

            return result.Code switch
            {
                200 or 201 => Ok(result),
                400 => BadRequest(result),
                401 => Unauthorized(result),
                404 => NotFound(result),
                _ => StatusCode(500, result)
            };
        }


        // GET: api/MissionApi/5
        [HttpGet("{id}")]
        public async Task<MissionDTO> GetMission(int id)
        {
            MissionDTO? MDTO = await _context.Mission.Select(m=>new MissionDTO
            {
                MissionId = m.MissionId,
                MissionName = m.MissionName,
                MissionDetail = m.MissionDetail,
                TargetType = m.TargetType,
                TargetCount = m.TargetCount,
                RewardPoints = m.RewardPoints,
                RewardExp = m.RewardExp,
                ResetType = m.ResetType,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt
            }).FirstOrDefaultAsync(m=>m.MissionId == id);

            return MDTO;
        }

        // PUT: api/MissionApi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ResultDTO> PutMission(int id, MissionDTO MDTO)
        {
            if (id != MDTO.MissionId)
            {
                return new ResultDTO { Ok = false, Code = 400 };
            }

            Mission m = await _context.Mission.FindAsync(id);
            if (m != null)
            {
                m.MissionName = MDTO.MissionName;
                m.MissionDetail = MDTO.MissionDetail;
                m.TargetType = MDTO.TargetType;
                m.TargetCount = MDTO.TargetCount;
                m.RewardPoints = MDTO.RewardPoints;
                m.RewardExp = MDTO.RewardExp;
                m.ResetType = MDTO.ResetType;
                m.IsActive = MDTO.IsActive;
                // 只有當前端真的有傳新時間才更新，否則保留資料庫原始時間
                m.CreatedAt = MDTO.CreatedAt ?? m.CreatedAt;

                _context.Entry(m).State = EntityState.Modified;

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

        // POST: api/MissionApi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ResultDTO> PostMission(MissionDTO MDTO)
        {
            Mission m = new Mission
            {
                MissionName = MDTO.MissionName,
                MissionDetail = MDTO.MissionDetail,
                TargetType = MDTO.TargetType,
                TargetCount = MDTO.TargetCount,
                RewardPoints = MDTO.RewardPoints,
                RewardExp = MDTO.RewardExp,
                ResetType = MDTO.ResetType,
                IsActive = true,           // 新增時預設啟用
                CreatedAt = DateTime.Now   // 強制後端時間
            };
            _context.Mission.Add(m);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 204 //NoContent
            };
        }

        // DELETE: api/MissionApi/5
        [HttpDelete("{id}")]
        public async Task<ResultDTO> DeleteMission(int id)
        {
            var mission = await _context.Mission.FindAsync(id);
            if (mission == null)
            {
                return new ResultDTO { Ok = false, Code = 404 }; //id不存在顯示404
            }
            try
            {
                _context.Mission.Remove(mission);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return new ResultDTO { Ok = false, Code = 500 }; //id存在捕捉例外顯示500
            }        
            return new ResultDTO { Ok = true, Code = 204 };
        }

        //private bool MissionExists(int id)
        //{
        //    return _context.Mission.Any(e => e.MissionId == id);
        //}
    }
}
