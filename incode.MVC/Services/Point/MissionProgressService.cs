using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.Services.Point
{
    public class MissionProgressService : IMissionProgressService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;
        private readonly IMissionService _missionService;
        private readonly ILogger<MissionProgressService> _logger;

        public MissionProgressService(incodedatabaseContext context, IPointLogService pointLogService, IMissionService missionService, ILogger<MissionProgressService> logger)
        {
            _context = context;
            _pointLogService = pointLogService;
            _missionService = missionService;
            _logger = logger;
        }

        public async Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId)
        {
            var data = await _context.MissionProgress
                .AsNoTracking()
                .Include(p => p.Mission)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            // 已領過獎的一次性任務（任何一筆進度領過就不能再領）
            var rewardedOneTime = data
                .Where(p => p.Mission != null && p.RewardedAt.HasValue && MissionResetTypes.IsOneTime(p.Mission.ResetType))
                .Select(p => p.MissionId)
                .ToHashSet();

            return data.Select(p =>
            {
                var m = p.Mission;
                if (m == null) return null!;

                bool isCompleted = p.CurrentCount >= m.TargetCount;
                // 與 ClaimRewardAsync 相同的規則：每筆進度只能領一次；一次性任務任何一筆領過就不能再領
                bool canClaim = isCompleted && !p.RewardedAt.HasValue && p.Status != "rewarded" && m.IsActive
                                && !(MissionResetTypes.IsOneTime(m.ResetType) && rewardedOneTime.Contains(m.MissionId));

                return new UserMissionViewDTO
                {
                    MissionProgressId = p.MissionProgressId,
                    MissionId = m.MissionId,
                    MissionName = m.MissionName,
                    MissionDetail = m.MissionDetail,
                    TargetType = m.TargetType,
                    TargetCount = m.TargetCount,
                    RewardPoints = m.RewardPoints,
                    RewardExp = m.RewardExp,
                    ResetType = m.ResetType,
                    Category = m.Category,
                    Difficulty = m.Difficulty,
                    EstimatedDuration = m.EstimatedDuration,
                    IsFeatured = m.IsFeatured ?? false,
                    IsActive = m.IsActive,
                    CurrentCount = p.CurrentCount,
                    Status = p.Status ?? "ongoing",
                    AcceptedAt = p.AcceptedAt,
                    CompletedAt = p.CompletedAt,
                    RewardedAt = p.RewardedAt,
                    ProgressPercentage = m.TargetCount > 0 ? Math.Min(100.0, (double)p.CurrentCount / m.TargetCount * 100) : 0,
                    CanClaimReward = canClaim,
                    DisplayStatus = p.Status switch
                    {
                        "completed" => canClaim ? "可領取獎勵" : "已完成",
                        "rewarded" => "已領取",
                        _ => isCompleted ? "已達標" : "進行中"
                    }
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.AcceptedAt)
            .ToList();
        }

        public async Task<ResultDTO> ClaimRewardAsync(int userId, int progressId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 先鎖定會員再讀取 RewardedAt：同時送出的領獎請求會排隊，第二個會看到已領取
                await _pointLogService.LockUsersAsync(userId);

                var progress = await _context.MissionProgress
                    .Include(p => p.Mission)
                    .FirstOrDefaultAsync(p => p.MissionProgressId == progressId && p.UserId == userId);

                if (progress == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "找不到您的任務進度，或無權限" };

                var mission = progress.Mission;
                if (mission == null)
                    return new ResultDTO { Ok = false, Code = 500, Message = "任務資料遺失" };

                if (!mission.IsActive)
                    return new ResultDTO { Ok = false, Code = 400, Message = "任務已停用" };

                if (progress.CurrentCount < mission.TargetCount)
                    return new ResultDTO
                    {
                        Ok = false,
                        Code = 400,
                        Message = $"進度未達標 ({progress.CurrentCount}/{mission.TargetCount})"
                    };

                // 一次性任務：同一會員任何一筆進度領過就不能再領（改版前每天會建立新進度，可能有多筆）
                if (MissionResetTypes.IsOneTime(mission.ResetType) &&
                    await _context.MissionProgress.AnyAsync(p =>
                        p.UserId == userId && p.MissionId == mission.MissionId && p.RewardedAt != null))
                    return new ResultDTO { Ok = false, Code = 400, Message = "此任務已領取過獎勵" };

                // 每筆進度只屬於一個週期（下個週期會建立新的進度），領過一次就不能再領。
                // 原本只比對「領獎時間是否在本週期」：昨天已領過的每日任務進度，今天可以再領一次，
                // 而且每天都能把所有舊進度再領一遍（第三次全專案檢查 45）
                if (progress.RewardedAt.HasValue || progress.Status == "rewarded")
                    return new ResultDTO { Ok = false, Code = 400, Message = "此任務進度已領取過獎勵" };

                progress.RewardedAt = DateTime.Now;
                progress.Status = "rewarded";

                // 建議補上顯示資訊（可選）
                string subTitle = $"任務：{mission.MissionName ?? $"Mission #{mission.MissionId}"}";

                var success = await _pointLogService.ChangePointsAsync(
                    userId,
                    mission.RewardPoints,
                    "mission",
                    mission.MissionId,
                    PointType.IncomeMission,
                    displaySource: "任務獎勵",           // 建議加上
                    displaySubTitle: subTitle            // 建議加上
                );

                if (!success)
                {
                    await tx.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "點數發放失敗" };
                }

                await _missionService.TriggerMissionAsync(userId, "mission_complete", 1);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO { Ok = true, Code = 200, Message = "獎勵領取成功" };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "任務領獎失敗 - UserId: {UserId}", userId);
                return new ResultDTO { Ok = false, Code = 500, Message = "領取失敗，請稍後再試" };
            }
        }

    }
}
