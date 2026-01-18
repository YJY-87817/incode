using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using incode2.DTO;

namespace incode2.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissionApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public MissionApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/MissionApi
        [HttpGet]
        public async Task<IEnumerable<MissionDTO>> GetMission()
        {
            return _context.Mission.Select(m=>new MissionDTO
            {
                MissionId = m.MissionId,
                MissionName = m.MissionName,
                MissionDetail = m.MissionDetail,
                TargetType = m.TargetType,
                TargetCount = m.TargetCount,
                RewardPoints = m.RewardPoints,
                RewardExp = m.RewardExp,
                ResetType = m.ResetType
            });
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
                ResetType = m.ResetType
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
                m.MissionId = MDTO.MissionId;
                m.MissionName = MDTO.MissionName;
                m.MissionDetail = MDTO.MissionDetail;
                m.TargetType = MDTO.TargetType;
                m.TargetCount = MDTO.TargetCount;
                m.RewardPoints = MDTO.RewardPoints;
                m.RewardExp = MDTO.RewardExp;
                m.ResetType = MDTO.ResetType;

                _context.Entry(MDTO).State = EntityState.Modified;

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
                ResetType = MDTO.ResetType
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
