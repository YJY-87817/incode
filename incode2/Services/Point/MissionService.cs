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

        //MissionId 是資料庫自動遞增的主鍵
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

        // Services/Point/MissionService.cs
        public async Task<ResultDTO> ClaimMissionAsync(int missionId, int userId)
        {
            var mission = await _context.Mission
                .FirstOrDefaultAsync(m => m.MissionId == missionId && m.IsActive);

            if (mission == null)
            {
                return new ResultDTO { Ok = false, Code = 404, Message = "任務不存在或已停用" };
            }

            // 檢查是否已經有該任務的進度記錄（避免重複領取）
            var existingProgress = await _context.MissionProgress
                .FirstOrDefaultAsync(mp => mp.UserId == userId && mp.MissionId == missionId);

            if (existingProgress != null)
            {
                // 如果已經存在，根據狀態判斷
                if (existingProgress.Status == "ongoing" || existingProgress.Status == "completed" || existingProgress.Status == "rewarded")
                {
                    return new ResultDTO { Ok = false, Code = 400, Message = "您已領取或完成此任務" };
                }
                // 如果是 falsed / cancelled，可以允許重新領取（視業務決定）
                // 這裡假設不允許，直接拒絕
                return new ResultDTO { Ok = false, Code = 400, Message = "您已領取過此任務" };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 更新任務總參與人數（可選，如果前端需要顯示總人數）
                mission.ParticipantCount = (mission.ParticipantCount ?? 0) + 1;

                // 建立新的 MissionProgress 記錄
                var missionProgress = new MissionProgress
                {
                    UserId = userId,
                    MissionId = missionId,
                    CurrentCount = 0,                    // 初始進度為 0
                    Status = "ongoing",                  // 領取後進入進行中
                    AcceptedAt = DateTime.UtcNow,
                    // completed_at、rewarded_at 留空，等後續完成/領獎時更新
                };

                _context.MissionProgress.Add(missionProgress);

                // 如果任務是一次性（無需計數），可以直接視為 completed（視業務）
                // 例如：每日登入任務 → 領取即完成
                // if (mission.TargetType == "once")
                // {
                //     missionProgress.Status = "completed";
                //     missionProgress.CompletedAt = DateTime.UtcNow;
                // }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "領取成功",
                    Data = new
                    {
                        MissionId = missionId,
                        ParticipantCount = mission.ParticipantCount,
                        ProgressId = missionProgress.MissionProgressId  // 可回傳進度 ID 給前端
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // 建議記錄錯誤（生產環境）
                // _logger.LogError(ex, "領取任務失敗 - MissionId: {MissionId}, UserId: {UserId}", missionId, userId);
                return new ResultDTO { Ok = false, Code = 500, Message = "系統錯誤，請稍後再試" };
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
                CreatedAt = DateTime.UtcNow
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
