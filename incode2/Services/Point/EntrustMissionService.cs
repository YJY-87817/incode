using Google.GenAI.Types;
using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
  public class EntrustMissionService : IEntrustMissionService
  {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;
        private readonly IPointReserveService _pointReserveService;

        public EntrustMissionService(incodedatabaseContext context,
            IPointLogService pointLogService,
            IPointReserveService pointReserveService) 
        { 
            _context = context; 
            _pointLogService = pointLogService;
            _pointReserveService = pointReserveService;
        }

        //拿全部委託任務資料
    public async Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync()
    {
            var now = DateTime.Now;
            return await _context.EntrustMission
          .Where(em => em.IsActive
          && (em.Status == "open" || em.Status == "in_progress")
          && (!em.Deadline.HasValue || em.Deadline.Value >= now)) // 過濾過期
          .Select(em => new EntrustMissionDTO
          {
            EntrustMissionId = em.EntrustMissionId,
            UserId = em.UserId,
            ContentId = em.ContentId,
            Title = em.Title,
            Detail = em.Detail,
            RewardPoints = em.RewardPoints,
            Category = em.Category ?? "其他",
            Deadline = em.Deadline,
            Status = em.Status ?? "open",
            IsUrgent = em.IsUrgent ?? false,
            IsActive = em.IsActive,
            CreatedAt = em.CreatedAt,
            UpdatedAt = em.UpdatedAt,
            Username = em.User.Username,
            ImageUrl = em.User.ImageUrl
          })
          .OrderByDescending(em => em.IsUrgent)      // 急件優先
          .ThenByDescending(em => em.CreatedAt)      // 新發布優先
          .ToListAsync();
    }

        //接取委託任務
    public async Task<ResultDTO> ClaimEntrustMissionAsync(int entrustMissionId, int userId)
    {
      using var transaction = await _context.Database.BeginTransactionAsync();
      try
      {
        // 鎖定該筆委託任務記錄
        var mission = await _context.EntrustMission
            .FromSqlRaw("SELECT * FROM entrust_mission WITH (UPDLOCK, ROWLOCK) WHERE entrust_mission_id = {0}", entrustMissionId)
            .FirstOrDefaultAsync();

        if (mission == null)
        {
          return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };
        }

        if (!mission.IsActive)
        {
          return new ResultDTO { Ok = false, Code = 410, Message = "該委託任務已下架" };
        }

        if (!string.Equals(mission.Status, "open", StringComparison.OrdinalIgnoreCase))
        {
          return new ResultDTO { Ok = false, Code = 400, Message = $"任務狀態為 {mission.Status}，無法接取" };
        }

        if (mission.Deadline.HasValue && mission.Deadline.Value < DateTime.Now)
        {
          return new ResultDTO { Ok = false, Code = 400, Message = "任務已過截止時間" };
        }

        // 防同一人重複接取「這個」任務（保留原判斷）
        bool alreadyClaimedThis = await _context.EntrustMissionProgress
            .AnyAsync(p => p.EntrustMissionId == entrustMissionId && p.UserId == userId);

        if (alreadyClaimedThis)
        {
          return new ResultDTO { Ok = false, Code = 409, Message = "您已經接取過此任務" };
        }

        // 新增進度記錄
        var progress = new EntrustMissionProgress
        {
          UserId = userId,
          EntrustMissionId = entrustMissionId,
          Status = "ongoing",
          AcceptedAt = DateTime.Now
        };

        _context.EntrustMissionProgress.Add(progress);

        // 更新主表
        mission.Status = "in_progress";
        mission.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        string discussionUrl = $"https://localhost:5173/forum/{mission.ContentId}";

        return new ResultDTO
        {
          Ok = true,
          Code = 200,
          Message = "成功接取委託任務",
          Data = new
          {
            EntrustMissionId = entrustMissionId,
            Status = mission.Status,
            AcceptedAt = progress.AcceptedAt,
            DiscussionUrl = discussionUrl
          }
        };
      }
      catch (Exception ex)
      {
        await transaction.RollbackAsync();
        // _logger.LogError(ex, "接取委託任務失敗 - MissionId: {Id}, UserId: {UserId}", entrustMissionId, userId);
        return new ResultDTO { Ok = false, Code = 500, Message = "接取任務失敗，請稍後再試" };
      }
    }

    public async Task<EntrustMissionDTO?> GetEntrustMissionByIdAsync(int id)
    {
      return await _context.EntrustMission
          .Where(em => em.EntrustMissionId == id && em.IsActive)  // 建議加上 IsActive 過濾
          .Include(em => em.User)
          .Include(em => em.Content)
          .Select(em => new EntrustMissionDTO
          {
            EntrustMissionId = em.EntrustMissionId,
            UserId = em.UserId,
            ContentId = em.ContentId,
            Title = em.Title,
            Detail = em.Detail,
            RewardPoints = em.RewardPoints,
            Category = em.Category ?? "其他",
            Deadline = em.Deadline,
            Status = em.Status ?? "其他",
            IsUrgent = em.IsUrgent ?? false,
            IsActive = em.IsActive,
            CreatedAt = em.CreatedAt,
            UpdatedAt = em.UpdatedAt,
          })
          .FirstOrDefaultAsync();
    }
        // 發布委託
        public async Task<ResultDTO> CreateEntrustMissionAsync(EntrustMissionDTO dto)
        {
            int userId = dto.UserId;
            DateTime now = DateTime.Now;

            // ========= 基本業務驗證 =========
            if (dto.RewardPoints <= 0)
                return new ResultDTO { Ok = false, Code = 400, Message = "賞金必須大於 0" };

            if (dto.ContentId <= 0)
                return new ResultDTO { Ok = false, Code = 400, Message = "必須選擇關聯內容" };

            if (dto.Deadline.HasValue && dto.Deadline < DateTime.Now)
                return new ResultDTO { Ok = false, Code = 400, Message = "截止時間不能是過去時間" };

            // ========= Content 是否存在 =========
            bool contentExists = await _context.Content
                .AsNoTracking()
                .AnyAsync(c => c.ContentId == dto.ContentId);

            if (!contentExists)
                return new ResultDTO { Ok = false, Code = 404, Message = "指定的內容不存在" };

            // ========= 使用者是否存在 =========
            bool userExists = await _context.User
                .AsNoTracking()
                .AnyAsync(u => u.UserId == userId);

            if (!userExists)
                return new ResultDTO { Ok = false, Code = 404, Message = "使用者不存在" };

            // ========= Transaction =========
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 建立委託任務
                var entrustMission = new EntrustMission
                {
                    UserId = userId,
                    ContentId = dto.ContentId,
                    Title = dto.Title,
                    Detail = dto.Detail ?? "無詳細說明",
                    RewardPoints = dto.RewardPoints,
                    Category = dto.Category,
                    Deadline = dto.Deadline,
                    Status = "open",
                    IsUrgent = dto.IsUrgent,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.EntrustMission.Add(entrustMission);
                // 先 Save 只為了拿 ID
                await _context.SaveChangesAsync();

                // 預扣點數 (第三步)
                var reserveSuccess = await _pointReserveService.ReserveAsync(
                    userId,
                    dto.RewardPoints,
                    entrustMission.EntrustMissionId
                );

                if (!reserveSuccess)
                {
                    await tx.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "點數不足或預扣失敗" };
                }

                //再 Save 預扣 + 點數 log
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 201,
                    Message = "委託任務建立成功",
                    Data = new { EntrustMissionId = entrustMission.EntrustMissionId }
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var realMessage = ex.InnerException?.Message ?? ex.Message;
                return new ResultDTO
                {
                    Ok = false,
                    Code = 500,
                    Message = $"除錯訊息: {realMessage}"
                };
            }
        }

        //取消委託任務時退還預扣點數
        public async Task<ResultDTO> CancelEntrustMissionAsync(int entrustMissionId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var mission = await _context.EntrustMission
                    .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId && em.IsActive);

                if (mission == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

                if (mission.UserId != userId)
                    return new ResultDTO { Ok = false, Code = 403, Message = "只有任務發起者可以取消任務" };

                // 1️⃣ 更新任務狀態
                mission.Status = "cancelled";
                mission.UpdatedAt = DateTime.Now;

                // 2️⃣ 退還預扣點數
                var releaseSuccess = await _pointReserveService.ReleaseAsync(
                    mission.UserId,
                    entrustMissionId
                );

                if (!releaseSuccess)
                {
                    await transaction.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "退還預扣點數失敗" };
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "委託任務已取消，預扣點數已退回"
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultDTO
                {
                    Ok = false,
                    Code = 500,
                    Message = ex.Message
                };
            }
        }

    }
}
