using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
    public class EntrustMissionService : IEntrustMissionService
    {
        private readonly incodedatabaseContext _context;
        public EntrustMissionService(incodedatabaseContext context) => _context = context;

        public async Task<IEnumerable<EntrustMissionDTO>> GetAllAsync()
        {
            return await _context.EntrustMission.Select(em => new EntrustMissionDTO
            {
                EntrustMissionId = em.EntrustMissionId,
                UserId = em.UserId,
                ContentId = em.ContentId,
                Title = em.Title,
                Detail = em.Detail,
                RewardPoints = em.RewardPoints,
                MaxParticipants = em.MaxParticipants,
                Status = em.Status,
                IsActive = em.IsActive,
                CreatedAt = em.CreatedAt
            }).ToListAsync();
        }

        public async Task<EntrustMissionDTO> GetByIdAsync(int id)
        {
            return await _context.EntrustMission
                .Where(em => em.EntrustMissionId == id)
                .Select(em => new EntrustMissionDTO { /* 同上映射 */ })
                .FirstOrDefaultAsync();
        }

        public async Task<ResultDTO> CreateAsync(EntrustMissionDTO dto)
        {
            var em = new EntrustMission
            {
                UserId = dto.UserId,
                ContentId = dto.ContentId,
                Title = dto.Title,
                Detail = dto.Detail,
                RewardPoints = dto.RewardPoints,
                MaxParticipants = dto.MaxParticipants,
                Status = dto.Status,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt ?? DateTime.Now
            };
            _context.EntrustMission.Add(em);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 201 };
        }

        public async Task<ResultDTO> UpdateAsync(int id, EntrustMissionDTO dto)
        {
            var em = await _context.EntrustMission.FindAsync(id);
            if (em == null) return new ResultDTO { Ok = false, Code = 404 };

            em.Title = dto.Title;
            em.Detail = dto.Detail;
            em.RewardPoints = dto.RewardPoints;
            em.MaxParticipants = dto.MaxParticipants;
            em.Status = dto.Status;
            em.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }

        public async Task<ResultDTO> DeleteAsync(int id)
        {
            var em = await _context.EntrustMission.FindAsync(id);
            if (em == null) return new ResultDTO { Ok = false, Code = 404 };
            _context.EntrustMission.Remove(em);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }
    }
}
