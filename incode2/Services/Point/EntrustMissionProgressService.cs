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
                SubmittedAt = emg.SubmittedAt,
                ApprovedAt = emg.ApprovedAt,
                RewardedAt = emg.RewardedAt
            }).ToListAsync();
        }

        // 取得目前登入者的所有委託任務進度（含任務標題、賞金等資訊）
        public async Task<IEnumerable<EntrustMissionProgressDTO>> GetMyProgressAsync(int userId)
        {
            return await _context.EntrustMissionProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.AcceptedAt)
                .Select(p => new EntrustMissionProgressDTO
                {
                    EntrustMissionProgressId = p.EntrustMissionProgressId,
                    UserId = p.UserId,
                    EntrustMissionId = p.EntrustMissionId,
                    Status = p.Status,
                    AcceptedAt = p.AcceptedAt,
                    SubmittedAt = p.SubmittedAt,
                    ApprovedAt = p.ApprovedAt,
                    RewardedAt = p.RewardedAt,

                    // 關聯任務資訊
                    Title = p.EntrustMission.Title,
                    Detail = p.EntrustMission.Detail,
                    Category = p.EntrustMission.Category,
                    RewardPoints = p.EntrustMission.RewardPoints,
                    // 如果需要更多欄位，例如發起者、截止時間等，可繼續補充
                })
                .ToListAsync();
        }
    }
}
