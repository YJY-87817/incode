using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.Services.Point
{
    public class MissionService : IMissionService
    {
        private readonly incodedatabaseContext _context;

        public MissionService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MissionDTO>> GetAllMissionAsync()
        {
            return await _context.Mission
                .Where(m => m.IsActive)
                .Select(m => new MissionDTO
                {
                    MissionId = m.MissionId,
                    MissionName = m.MissionName,
                    MissionDetail = m.MissionDetail,
                    TargetType = m.TargetType,
                    TargetCount = m.TargetCount,
                    RewardPoints = m.RewardPoints,
                    RewardExp = m.RewardExp,
                    Category = m.Category,
                    Difficulty = m.Difficulty,
                    EstimatedDuration = m.EstimatedDuration,
                    ResetType = m.ResetType,
                    ParticipantCount = m.ParticipantCount,
                    IsFeatured = m.IsFeatured,
                    LastResetAt = m.LastResetAt,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<MissionDTO?> GetMissionByIdAsync(int id)
        {
            return await _context.Mission
                .Where(m => m.MissionId == id && m.IsActive)
                .Select(m => new MissionDTO
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
                    ParticipantCount = m.ParticipantCount,
                    IsFeatured = m.IsFeatured,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        // 領取任務（適用於每日簽到、一次性任務等）
        public async Task<ResultDTO> ClaimMissionAsync(int missionId, int userId)
        {
            var mission = await _context.Mission
                .FirstOrDefaultAsync(m => m.MissionId == missionId && m.IsActive);

            if (mission == null)
                return new ResultDTO { Ok = false, Code = 404, Message = "任務不存在或已停用" };

            var (cycleStart, cycleEnd) = GetCycleRange(mission.ResetType);

            // 額外檢查：確保日期在 SQL 允許範圍內
            if (cycleStart < new DateTime(1753, 1, 1) || cycleEnd > new DateTime(9999, 12, 31))
            {
                // 強制修正為安全範圍，或拋出明確錯誤
                cycleStart = DateTime.Now.Date;
                cycleEnd = cycleStart.AddYears(10); 
            }

            // 先檢查是否存在
            var existing = await _context.MissionProgress
                .AnyAsync(p => p.UserId == userId &&
                               p.MissionId == missionId &&
                               p.CycleStartAt == cycleStart);

            if (existing)
            {
                // 可選：如果業務允許，可改成回成功訊息「已領取」
                return new ResultDTO { Ok = false, Code = 409, Message = "本週期已領取過此任務" };
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                mission.ParticipantCount = (mission.ParticipantCount ?? 0) + 1;

                var progress = new MissionProgress
                {
                    UserId = userId,
                    MissionId = missionId,
                    CurrentCount = 0,
                    Status = "ongoing",
                    AcceptedAt = DateTime.Now,
                    CycleStartAt = cycleStart,
                    CycleEndAt = cycleStart.AddDays(7) // 保守設一個範圍，實際以 resetType 為主
                };

                // 簽到類任務自動完成
                if (mission.TargetCount == 1 &&
                    string.Equals(mission.TargetType, "check-in", StringComparison.OrdinalIgnoreCase))
                {
                    progress.CurrentCount = 1;
                    progress.Status = "completed";
                    progress.CompletedAt = DateTime.Now;
                }

                _context.MissionProgress.Add(progress);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "任務領取成功",
                    Data = new { MissionId = missionId, Status = progress.Status }
                };
            }
            catch (DbUpdateException dbEx)
            {
                await tx.RollbackAsync();
                // 可記 log：重複索引違反 → 視為已領取
                if (dbEx.InnerException?.Message.Contains("unique") ?? false)
                {
                    return new ResultDTO { Ok = false, Code = 409, Message = "本週期已領取過（資料庫驗證）" };
                }
                return new ResultDTO { Ok = false, Code = 500, Message = "資料庫錯誤" };
            }
            catch
            {
                await tx.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = "系統錯誤，請稍後再試" };
            }
        }

        // 建立任務（後台用）
        public async Task<ResultDTO> CreateMissionAsync(MissionDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MissionName))
                return new ResultDTO { Ok = false, Code = 400, Message = "任務名稱為必填" };

            var mission = new Mission
            {
                MissionName = dto.MissionName,
                MissionDetail = dto.MissionDetail ?? "",
                TargetType = dto.TargetType ?? "general",
                TargetCount = dto.TargetCount > 0 ? dto.TargetCount : 1,
                RewardPoints = dto.RewardPoints,
                RewardExp = dto.RewardExp,
                Category = dto.Category ?? "其他",
                Difficulty = dto.Difficulty ?? "簡單",
                EstimatedDuration = dto.EstimatedDuration,
                ResetType = dto.ResetType ?? "daily",
                ParticipantCount = 0,
                IsFeatured = dto.IsFeatured ?? false,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Mission.Add(mission);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 201,
                Message = "任務建立成功",
                Data = new { MissionId = mission.MissionId }
            };
        }

        // 週期計算輔助方法
        private (DateTime Start, DateTime End) GetCycleRange(string? resetType)
        {
            var now = DateTime.Now;
            var date = now.Date;

            DateTime start;
            DateTime end;

            switch (resetType?.ToLowerInvariant())
            {
                case "daily":
                    start = date;
                    end = date.AddDays(1).AddTicks(-1);
                    break;

                case "weekly":
                    var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    start = date.AddDays(-diff);
                    end = start.AddDays(7).AddTicks(-1);
                    break;

                case "monthly":
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.AddMonths(1).AddTicks(-1);
                    break;

                case "yearly":
                    start = new DateTime(now.Year, 1, 1);
                    end = start.AddYears(1).AddTicks(-1);
                    break;

                case "once":
                    // 一次性任務：用固定範圍，或用建立當天作為週期
                    start = now.Date;
                    end = new DateTime(9999, 12, 31, 23, 59, 59); // 最大值，避免溢位
                    break;

                default: // null、空字串或其他未知 → 視為一次性
                    start = now.Date;
                    end = new DateTime(9999, 12, 31, 23, 59, 59);
                    break;
            }

            return (start, end);
        }

        //事件入口
        public Task TriggerMissionAsync(int userId, string actionType, int increment = 1)
            => UpdateMissionProgressAsync(userId, actionType, (mission, progress, cycleStart, cycleEnd) =>
            {
                // 判斷是否可以累加：只對「每天簽到」這類一次性的做限制，
                // 其他 daily 任務（post, like, comment 等）與週期任務允許累計
                var resetType = (mission.ResetType ?? "none").ToLowerInvariant();
                bool canIncrement = !(resetType == "daily" && mission.TargetType == "login") || progress.CurrentCount == 0;
                return Task.FromResult(canIncrement ? progress.CurrentCount + increment : progress.CurrentCount);
            });

        public Task SyncMissionCountAsync(int userId, string actionType, Func<DateTime, DateTime, Task<int>> countInCycle)
            => UpdateMissionProgressAsync(userId, actionType, async (mission, progress, cycleStart, cycleEnd) =>
                Math.Max(progress.CurrentCount, await countInCycle(cycleStart, cycleEnd)));

        // 對所有符合 actionType 的有效任務：取得（或建立）本週期的進度，以 newCount 算出新的進度並判斷是否完成
        private async Task UpdateMissionProgressAsync(
            int userId,
            string actionType,
            Func<Mission, MissionProgress, DateTime, DateTime, Task<int>> newCount)
        {
            var now = DateTime.Now;
            // 抓所有符合 actionType 的有效任務，不限定週期
            var missions = await _context.Mission
                .Where(m => m.IsActive &&
                            m.TargetType == actionType)
                .ToListAsync();
            foreach (var mission in missions)
            {
                var (cycleStart, cycleEnd) = GetCycleRange(mission.ResetType);
                // 取得本週期的進度
                var progress = await _context.MissionProgress
                    .FirstOrDefaultAsync(p =>
                        p.UserId == userId &&
                        p.MissionId == mission.MissionId &&
                        p.CycleStartAt == cycleStart);
                // 如果今天/本週期還沒建立 → 建立新進度
                if (progress == null)
                {
                    progress = new MissionProgress
                    {
                        UserId = userId,
                        MissionId = mission.MissionId,
                        CurrentCount = 0,
                        Status = "ongoing",
                        AcceptedAt = now,
                        CycleStartAt = cycleStart,
                        CycleEndAt = cycleEnd
                    };
                    _context.MissionProgress.Add(progress);
                }

                progress.CurrentCount = await newCount(mission, progress, cycleStart, cycleEnd);

                // 自動完成判斷（已領獎的不改回 completed）
                if (progress.CurrentCount >= mission.TargetCount && progress.Status != "rewarded")
                {
                    progress.Status = "completed";
                    progress.CompletedAt ??= now;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
