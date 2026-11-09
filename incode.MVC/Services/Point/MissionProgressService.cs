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

        public async Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId)
        {
            var now = DateTime.Now;

            var data = await _context.MissionProgress
                .AsNoTracking()
                .Include(p => p.Mission)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return data.Select(p =>
            {
                var m = p.Mission;
                if (m == null) return null!;

                var resetType = (m.ResetType ?? "none").ToLowerInvariant();
                bool isCompleted = p.CurrentCount >= m.TargetCount;
                bool canClaim = isCompleted && !p.RewardedAt.HasValue && m.IsActive;

                // 已領取過，檢查是否同週期
                if (p.RewardedAt.HasValue)
                {
                    var last = p.RewardedAt.Value;
                    bool sameCycle = resetType switch
                    {
                        "daily" => last.Date == now.Date,
                        "weekly" => IsSameWeek(last, now),
                        "monthly" => last.Year == now.Year && last.Month == now.Month,
                        "yearly" => last.Year == now.Year,
                        "once" or "none" => true,
                        _ => false
                    };
                    canClaim = canClaim && !sameCycle;
                }

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

        public async Task<ResultDTO> IncrementProgressAsync(int userId, int missionId, int increment = 1)
        {
            if (increment <= 0) return new ResultDTO { Ok = false, Code = 400, Message = "增量必須大於 0" };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var progress = await _context.MissionProgress
                    .Include(p => p.Mission)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.MissionId == missionId);

                if (progress == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "尚未領取此任務" };

                var mission = progress.Mission!;
                if (progress.Status != "ongoing")
                    return new ResultDTO { Ok = false, Code = 400, Message = "任務狀態不允許更新" };

                progress.CurrentCount += increment;

                if (progress.CurrentCount >= mission.TargetCount && progress.Status != "completed")
                {
                    progress.Status = "completed";
                    progress.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "進度更新成功",
                    Data = new { CurrentCount = progress.CurrentCount, Status = progress.Status }
                };
            }
            catch
            {
                await tx.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = "更新失敗" };
            }
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

                if (progress.RewardedAt.HasValue)
                {
                    var resetType = (mission.ResetType ?? "none").ToLowerInvariant();
                    var last = progress.RewardedAt.Value;
                    bool sameCycle = resetType switch
                    {
                        "daily" => last.Date == DateTime.Now.Date,
                        "weekly" => IsSameWeek(last, DateTime.Now),
                        "monthly" => last.Year == DateTime.Now.Year && last.Month == DateTime.Now.Month,
                        "yearly" => last.Year == DateTime.Now.Year,
                        "once" or "none" => true,
                        _ => false
                    };

                    if (sameCycle)
                        return new ResultDTO { Ok = false, Code = 400, Message = "本週期已領取過獎勵" };
                }

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

        private bool IsSameWeek(DateTime d1, DateTime d2)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(d1, CalendarWeekRule.FirstDay, DayOfWeek.Monday) ==
                   cal.GetWeekOfYear(d2, CalendarWeekRule.FirstDay, DayOfWeek.Monday) &&
                   d1.Year == d2.Year;
        }

    }
}
