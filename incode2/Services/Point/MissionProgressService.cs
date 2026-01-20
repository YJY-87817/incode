using Microsoft.EntityFrameworkCore;
using incode2.Models;
using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public class MissionProgressService : IMissionProgressService
    {
        private readonly incodedatabaseContext _context;

        public MissionProgressService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 1. 取得所有進度
        public async Task<IEnumerable<MissionProgressDTO>> GetAllProgressAsync()
        {
            return await _context.MissionProgress.Select(mg => new MissionProgressDTO
            {
                MissionProgressId = mg.MissionProgressId,
                UserId = mg.UserId,
                MissionId = mg.MissionId,
                CurrentCount = mg.CurrentCount,
                Status = mg.Status,
                AcceptedAt = mg.AcceptedAt,
                CompletedAt = mg.CompletedAt,
                RewardedAt = mg.RewardedAt
            }).ToListAsync();
        }

        // 2. 根據 ID 取得特定進度 (這是你漏掉的部分)
        public async Task<MissionProgressDTO> GetProgressByIdAsync(int id)
        {
            return await _context.MissionProgress
                .Where(mg => mg.MissionProgressId == id)
                .Select(mg => new MissionProgressDTO
                {
                    MissionProgressId = mg.MissionProgressId,
                    UserId = mg.UserId,
                    MissionId = mg.MissionId,
                    CurrentCount = mg.CurrentCount,
                    Status = mg.Status,
                    AcceptedAt = mg.AcceptedAt,
                    CompletedAt = mg.CompletedAt,
                    RewardedAt = mg.RewardedAt
                }).FirstOrDefaultAsync();
        }

        // 3. 新增進度
        public async Task<ResultDTO> CreateProgressAsync(MissionProgressDTO dto)
        {
            var mg = new MissionProgress
            {
                UserId = dto.UserId,
                MissionId = dto.MissionId,
                CurrentCount = dto.CurrentCount,
                Status = dto.Status,
                AcceptedAt = dto.AcceptedAt ?? DateTime.Now // 若無則給當前時間
            };

            _context.MissionProgress.Add(mg);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 201 };
        }

        // 4. 更新進度
        public async Task<ResultDTO> UpdateProgressAsync(int id, MissionProgressDTO dto)
        {
            var mg = await _context.MissionProgress.FindAsync(id);
            if (mg == null) return new ResultDTO { Ok = false, Code = 404 };

            // 更新欄位
            mg.CurrentCount = dto.CurrentCount;
            mg.Status = dto.Status;
            mg.CompletedAt = dto.CompletedAt;
            mg.RewardedAt = dto.RewardedAt;

            try
            {
                await _context.SaveChangesAsync();
                return new ResultDTO { Ok = true, Code = 204 };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new ResultDTO { Ok = false, Code = 500 };
            }
        }

        // 5. 刪除進度
        public async Task<ResultDTO> DeleteProgressAsync(int id)
        {
            var mg = await _context.MissionProgress.FindAsync(id);
            if (mg == null) return new ResultDTO { Ok = false, Code = 404 };

            _context.MissionProgress.Remove(mg);
            await _context.SaveChangesAsync();
            return new ResultDTO { Ok = true, Code = 204 };
        }
    }
}