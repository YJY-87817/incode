using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
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
                }).ToListAsync();
        }

        //領取平台任務
        public async Task<ResultDTO> ClaimMissionAsync(int missionId, int userId)
        {
            var mission = await _context.Mission
                .FirstOrDefaultAsync(m => m.MissionId == missionId && m.IsActive);
            if (mission == null)
            {
                return new ResultDTO { Ok = false, Code = 404, Message = "任務不存在或已停用" };
            }
            // 計算週期
            DateTime cycleStart;
            DateTime cycleEnd;

            switch (mission.ResetType)
            {
                case "daily":
                    cycleStart = DateTime.Now.Date;
                    cycleEnd = cycleStart.AddDays(1);
                    break;

                case "weekly":
                    cycleStart = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
                    cycleEnd = cycleStart.AddDays(7);
                    break;

                case "yearly":
                    cycleStart = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfYear);
                    cycleEnd = cycleStart.AddDays(365);
                    break;

                case "once":
                default:
                    cycleStart = DateTime.MinValue;
                    cycleEnd = DateTime.MaxValue;
                    break;
            }

            // 查詢「本週期」是否已領取
            var existingProgress = await _context.MissionProgress.FirstOrDefaultAsync(mp =>
                mp.UserId == userId &&
                mp.MissionId == missionId &&
                mp.CycleStartAt == cycleStart
            );

            if (existingProgress != null)
            {
                return new ResultDTO { Ok = false, Code = 400, Message = "本週期已領取過此任務" };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                mission.ParticipantCount = (mission.ParticipantCount ?? 0) + 1;

                var missionProgress = new MissionProgress
                {
                    UserId = userId,
                    MissionId = missionId,
                    CurrentCount = 1,          // 每日簽到直接 +1
                    Status = "completed",      // 簽到即完成
                    AcceptedAt = DateTime.Now,
                    CompletedAt = DateTime.Now,
                    CycleStartAt = cycleStart,
                    CycleEndAt = cycleEnd
                };

                _context.MissionProgress.Add(missionProgress);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "簽到成功"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = "系統錯誤" };
            }
        }



        //IsActive、CreatedAt、UpdatedAt 不應該被前端任意修改
        public async Task<ResultDTO> CreateMissionAsync(MissionDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MissionName))
                return new ResultDTO { Ok = false, Code = 400, Message = "任務名稱為必填" };

            var mission = new Mission
            {
                MissionName = dto.MissionName,
                MissionDetail = dto.MissionDetail ?? "",
                TargetType = dto.TargetType ?? throw new ArgumentNullException(nameof(dto.TargetType)),
                TargetCount = dto.TargetCount,
                RewardPoints = dto.RewardPoints,
                RewardExp = dto.RewardExp,
                Category = dto.Category ?? throw new ArgumentNullException(nameof(dto.Category)),
                Difficulty = dto.Difficulty ?? "簡單",
                EstimatedDuration = dto.EstimatedDuration,
                ResetType = dto.ResetType ?? "none",
                ParticipantCount = 0,
                IsFeatured = dto.IsFeatured ?? false,
                LastResetAt = dto.LastResetAt,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Mission.Add(mission);
            await _context.SaveChangesAsync();

            return new ResultDTO
            {
                Ok = true,
                Code = 201,
                Message = "任務建立成功",
                Data = new { mission.MissionId }
            };
        }

        public async Task<ResultDTO> UpdateMissionAsync(int id, MissionDTO dto)
        {
            var mission = await _context.Mission.FindAsync(id);
            if (mission == null) return new ResultDTO { Ok = false, Code = 404 };

            mission.MissionName = dto.MissionName ?? mission.MissionName;
            mission.MissionDetail = dto.MissionDetail ?? mission.MissionDetail;
            mission.TargetType = dto.TargetType ?? mission.TargetType;
            mission.TargetCount = dto.TargetCount;
            mission.RewardPoints = dto.RewardPoints;
            mission.RewardExp = dto.RewardExp;
            mission.Category = dto.Category ?? mission.Category;
            mission.Difficulty = dto.Difficulty ?? mission.Difficulty;
            mission.EstimatedDuration = dto.EstimatedDuration ?? mission.EstimatedDuration;
            mission.ResetType = dto.ResetType ?? mission.ResetType;
            mission.ParticipantCount = dto.ParticipantCount ?? mission.ParticipantCount;
            mission.IsFeatured = dto.IsFeatured ?? mission.IsFeatured;
            mission.LastResetAt = dto.LastResetAt ?? mission.LastResetAt;

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
                    Category = m.Category,
                    Difficulty = m.Difficulty,
                    EstimatedDuration = m.EstimatedDuration,
                    ResetType = m.ResetType,
                    ParticipantCount = m.ParticipantCount,
                    IsFeatured = m.IsFeatured,
                    LastResetAt = m.LastResetAt,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}
