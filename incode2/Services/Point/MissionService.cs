using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
    public class MissionService : IMissionService
    {
        private readonly incodedatabaseContext _context;
        public MissionService(incodedatabaseContext context) => _context = context;

        public async Task<IEnumerable<MissionDTO>> GetAllMissionsAsync()
        {
            return await _context.Mission.Select(m => new MissionDTO
            {
                MissionId = m.MissionId,
                MissionName = m.MissionName,
                MissionDetail = m.MissionDetail,
                TargetType = m.TargetType,
                TargetCount = m.TargetCount,
                RewardPoints = m.RewardPoints,
                RewardExp = m.RewardExp,
                ResetType = m.ResetType
            }).ToListAsync();
        }

        public async Task<ResultDTO> CreateMissionAsync(MissionDTO dto)
        {
            var mission = new Mission
            {
                MissionName = dto.MissionName,
                MissionDetail = dto.MissionDetail,
                TargetType = dto.TargetType,
                TargetCount = dto.TargetCount,
                RewardPoints = dto.RewardPoints,
                RewardExp = dto.RewardExp,
                ResetType = dto.ResetType
            };
            _context.Mission.Add(mission);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 201 };
        }

        public async Task<ResultDTO> UpdateMissionAsync(int id, MissionDTO dto)
        {
            var mission = await _context.Mission.FindAsync(id);
            if (mission == null) return new ResultDTO { Ok = false, Code = 404 };

            mission.MissionName = dto.MissionName;
            mission.MissionDetail = dto.MissionDetail;
            mission.TargetType = dto.TargetType;
            mission.TargetCount = dto.TargetCount;
            mission.RewardPoints = dto.RewardPoints;
            mission.RewardExp = dto.RewardExp;
            mission.ResetType = dto.ResetType;

            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }

        public async Task<ResultDTO> DeleteMissionAsync(int id)
        {
            var mission = await _context.Mission.FindAsync(id);
            if (mission == null) return new ResultDTO { Ok = false, Code = 404 };

            _context.Mission.Remove(mission);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }

        public async Task<MissionDTO> GetMissionByIdAsync(int id)
        {
            return await _context.Mission
                .Where(m => m.MissionId == id)
                .Select(m => new MissionDTO
                {
                    MissionId = m.MissionId,
                    MissionName = m.MissionName,
                    MissionDetail = m.MissionDetail,
                    TargetType = m.TargetType,
                    TargetCount = m.TargetCount,
                    RewardPoints = m.RewardPoints,
                    RewardExp = m.RewardExp,
                    ResetType = m.ResetType
                })
                .FirstOrDefaultAsync();
        }
    }
}
