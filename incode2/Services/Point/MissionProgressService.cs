using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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


        // 進度更新方法
        public async Task<ResultDTO> IncrementProgressAsync(int userId, int missionId, int increment = 1)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var progress = await _context.MissionProgress
                    .Include(p => p.Mission)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.MissionId == missionId);

                if (progress == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "尚未領取此任務" };

                var mission = progress.Mission ?? throw new InvalidOperationException("任務資料遺失");

                if (progress.Status != "ongoing")
                    return new ResultDTO { Ok = false, Code = 400, Message = "任務狀態不允許更新進度" };

                progress.CurrentCount += increment;

                bool justCompleted = false;
                if (progress.CurrentCount >= mission.TargetCount && progress.Status != "completed")
                {
                    progress.Status = "completed";
                    progress.CompletedAt = DateTime.Now;
                    justCompleted = true;
                }

                await _context.SaveChangesAsync();

                // 可選：達標後自動領取（根據業務決定）
                if (justCompleted)
                {
                    // 建議：未來可從 Mission 表加欄位 IsAutoClaim 來控制
                    // 目前先註解，測試時可手動開啟
                    // var claimResult = await ClaimRewardAsync(userId, missionId);
                }

                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "進度更新成功",
                    Data = new { CurrentCount = progress.CurrentCount, Status = progress.Status }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // 建議：記錄 log，例如 _logger.LogError(ex, "更新進度失敗");
                return new ResultDTO { Ok = false, Code = 500, Message = "更新進度失敗" };
            }
        }

        // 取得「我的任務」列表（帶進度）
        public async Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId)
        {
            var rawData = await (from mp in _context.MissionProgress
                                 join m in _context.Mission on mp.MissionId equals m.MissionId
                                 where mp.UserId == userId
                                 select new { Mission = m, Progress = mp })
                                 .AsNoTracking()
                                 .ToListAsync();

            var now = DateTime.Now;

            return rawData.Select(x =>
            {
                var m = x.Mission;
                var mp = x.Progress;

                string resetType = (m.ResetType ?? "none").Trim().ToLowerInvariant();

                bool canClaim = mp.CurrentCount >= m.TargetCount && !mp.RewardedAt.HasValue && m.IsActive;

                if (canClaim && resetType != "none" && mp.RewardedAt.HasValue)
                {
                    var last = mp.RewardedAt.Value;
                    bool samePeriod = resetType switch
                    {
                        "daily" => last.Date == now.Date,
                        "weekly" => IsSameWeek(last, now),
                        "monthly" => last.Year == now.Year && last.Month == now.Month,
                        "yearly" => last.Year == now.Year,
                        "quarterly" => GetQuarter(last) == GetQuarter(now),
                        _ => false
                    };
                    canClaim = !samePeriod;
                }

                return new UserMissionViewDTO
                {
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

                    CurrentCount = mp.CurrentCount,
                    Status = mp.Status,
                    AcceptedAt = mp.AcceptedAt,
                    CompletedAt = mp.CompletedAt,
                    RewardedAt = mp.RewardedAt,

                    ProgressPercentage = m.TargetCount > 0
                        ? Math.Min(100.0, (double)mp.CurrentCount / m.TargetCount * 100)
                        : 0,

                    CanClaimReward = canClaim,

                    DisplayStatus = mp.Status switch
                    {
                        "ongoing" => mp.CurrentCount >= m.TargetCount ? "已達標（可領取）" : "進行中",
                        "completed" => canClaim ? "可領取獎勵" : "已完成",
                        "rewarded" => "已領取獎勵",
                        _ => mp.Status ?? "未知狀態"
                    }
                };
            })
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.AcceptedAt ?? DateTime.MinValue)
            .ToList();
        }

        public async Task<ResultDTO> CanClaimRewardAsync(int userId, int missionId)
        {
            var progress = await _context.MissionProgress
                .Include(p => p.Mission)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.MissionId == missionId);

            if (progress == null)
                return new ResultDTO { Ok = false, Code = 404, Message = "尚未接受此任務" };

            var mission = progress.Mission;
            if (mission == null)
                return new ResultDTO { Ok = false, Code = 500, Message = "任務資料遺失" };

            if (!mission.IsActive)
                return new ResultDTO { Ok = false, Code = 400, Message = "任務已停用" };

            var now = DateTime.Now;
            string resetType = (mission.ResetType ?? "none").Trim().ToLowerInvariant();

            // 已領取過，且為一次性任務 → 不可再領
            if (progress.RewardedAt.HasValue && resetType == "none")
                return new ResultDTO { Ok = false, Code = 400, Message = "此任務已領取過獎勵" };

            // 週期性任務 → 檢查是否在同一個週期已領過
            if (progress.RewardedAt.HasValue)
            {
                DateTime last = progress.RewardedAt.Value;
                bool samePeriod = resetType switch
                {
                    "daily" => last.Date == now.Date,
                    "weekly" => IsSameWeek(last, now),
                    "monthly" => last.Year == now.Year && last.Month == now.Month,
                    "yearly" => last.Year == now.Year,
                    "quarterly" => GetQuarter(last) == GetQuarter(now),
                    "none" => true,
                    _ => false
                };

                if (samePeriod)
                {
                    string periodName = resetType switch
                    {
                        "daily" => "今日",
                        "weekly" => "本週",
                        "monthly" => "本月",
                        "yearly" => "本年",
                        "quarterly" => "本季",
                        "none" => "此任務",
                        _ => "此週期"
                    };

                    return new ResultDTO { Ok = false, Code = 400, Message = $"{periodName}已領取過" };
                }
            }

            // 進度是否達標
            if (progress.CurrentCount < mission.TargetCount)
            {
                return new ResultDTO
                {
                    Ok = false,
                    Code = 400,
                    Message = $"進度未達標 ({progress.CurrentCount} / {mission.TargetCount})"
                };
            }

            return new ResultDTO { Ok = true, Code = 200, Message = "可以領取" };
        }

        public async Task<ResultDTO> ClaimRewardAsync(int userId, int missionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 取得任務進度
                var progress = await _context.MissionProgress
                    .Include(p => p.Mission)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.MissionId == missionId);

                if (progress == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "任務進度不存在" };

                // 是否可領取（狀態 / 次數 / 條件）
                var canClaim = await CanClaimRewardAsync(userId, missionId);
                if (!canClaim.Ok)
                    return canClaim;

                // 標記任務為已領取
                progress.RewardedAt = DateTime.Now;
                progress.Status = "Rewarded";

                // 發放點數（統一走 ChangePointsAsync）
                var success = await _pointLogService.ChangePointsAsync(
                    userId,
                    progress.Mission.RewardPoints,
                    "mission",
                    missionId,
                    PointType.Mission
                );

                if (!success)
                {
                    await transaction.RollbackAsync();
                    return new ResultDTO
                    {
                        Ok = false,
                        Code = 400,
                        Message = "點數發放失敗"
                    };
                }

                // 一次性提交
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "獎勵領取成功，已記錄點數"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultDTO
                {
                    Ok = false,
                    Code = 500,
                    Message = $"領取獎勵失敗：{ex.Message}"
                };
            }
        }

        // 輔助方法（保持不變）
        private bool IsSameWeek(DateTime dt1, DateTime dt2)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(dt1, CalendarWeekRule.FirstDay, DayOfWeek.Monday) ==
                   cal.GetWeekOfYear(dt2, CalendarWeekRule.FirstDay, DayOfWeek.Monday) &&
                   dt1.Year == dt2.Year;
        }

        private int GetQuarter(DateTime dt)
        {
            return ((dt.Month - 1) / 3) + 1;
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