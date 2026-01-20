using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
    public class EntrustMissionProgressService : IEntrustMissionProgressService
    {
        private readonly incodedatabaseContext _context;
        public EntrustMissionProgressService(incodedatabaseContext context) => _context = context;

        public async Task<IEnumerable<EntrustMissionProgressDTO>> GetAllProgressAsync()
        {
            return await _context.EntrustMissionProgress.Select(emg => new EntrustMissionProgressDTO
            {
                EntrustMissionProgressId = emg.EntrustMissionProgressId,
                UserId = emg.UserId,
                EntrustMissionId = emg.EntrustMissionId,
                Status = emg.Status,
                AcceptedAt = emg.AcceptedAt,
                CompletedAt = emg.CompletedAt,
                RewardedAt = emg.RewardedAt
            }).ToListAsync();
        }

        public async Task<EntrustMissionProgressDTO> GetProgressByIdAsync(int id)
        {
            return await _context.EntrustMissionProgress
                .Where(emg => emg.EntrustMissionProgressId == id)
                .Select(emg => new EntrustMissionProgressDTO { /* 同上映射 */ })
                .FirstOrDefaultAsync();
        }

        public async Task<ResultDTO> CreateProgressAsync(EntrustMissionProgressDTO dto)
        {
            var emg = new EntrustMissionProgress
            {
                UserId = dto.UserId,
                EntrustMissionId = dto.EntrustMissionId,
                Status = dto.Status,
                AcceptedAt = dto.AcceptedAt ?? DateTime.Now
            };
            _context.EntrustMissionProgress.Add(emg);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 201 };
        }

        public async Task<ResultDTO> UpdateProgressAsync(int id, EntrustMissionProgressDTO dto)
        {
            var emg = await _context.EntrustMissionProgress.FindAsync(id);
            if (emg == null) return new ResultDTO { Ok = false, Code = 404 };

            emg.Status = dto.Status;
            emg.CompletedAt = dto.CompletedAt;
            emg.RewardedAt = dto.RewardedAt;

            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }

        public async Task<ResultDTO> DeleteProgressAsync(int id)
        {
            var emg = await _context.EntrustMissionProgress.FindAsync(id);
            if (emg == null) return new ResultDTO { Ok = false, Code = 404 };
            _context.EntrustMissionProgress.Remove(emg);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }
    }
}
