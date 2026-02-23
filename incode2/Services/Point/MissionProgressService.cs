using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Services.Point
{
    public class MissionProgressService : IMissionProgressService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;

        public MissionProgressService(incodedatabaseContext context, IPointLogService pointLogService)
        {
            _context = context;
            _pointLogService = pointLogService;
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
                    return new ResultDTO { Ok = false, Code = 400, Message = $"進度未達標 ({progress.CurrentCount}/{mission.TargetCount})" };

                if (progress.RewardedAt.HasValue)
                {
                    var resetType = (mission.ResetType ?? "none").ToLowerInvariant();
                    var last = progress.RewardedAt.Value;
                    bool sameCycle = resetType switch
                    {
                        "daily" => last.Date == DateTime.UtcNow.Date,
                        "weekly" => IsSameWeek(last, DateTime.UtcNow),
                        "monthly" => last.Year == DateTime.UtcNow.Year && last.Month == DateTime.UtcNow.Month,
                        "yearly" => last.Year == DateTime.UtcNow.Year,
                        "once" or "none" => true,
                        _ => false
                    };

                    if (sameCycle)
                        return new ResultDTO { Ok = false, Code = 400, Message = "本週期已領取過獎勵" };
                }

                progress.RewardedAt = DateTime.UtcNow;
                progress.Status = "rewarded";

                var success = await _pointLogService.ChangePointsAsync(
                    userId,
                    mission.RewardPoints,
                    "mission",
                    mission.MissionId,
                    PointType.IncomeMission
                );

                if (!success)
                {
                    await tx.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "點數發放失敗" };
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO { Ok = true, Code = 200, Message = "獎勵領取成功" };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = $"領取失敗：{ex.Message}" };
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
