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
        public EntrustMissionService(incodedatabaseContext context,IPointLogService pointLogService) 
        { 
            _context = context; 
            _pointLogService = pointLogService;
        }

    public async Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync()
    {
      return await _context.EntrustMission
          .Where(em => em.IsActive && (em.Status == "open" || em.Status == "in_progress"))
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
            Status = em.Status ?? "open",
            IsUrgent = em.IsUrgent ?? false,
            IsActive = em.IsActive,
            CreatedAt = em.CreatedAt,
            UpdatedAt = em.UpdatedAt,
          })
          .OrderByDescending(em => em.IsUrgent)      // 急件優先
          .ThenByDescending(em => em.CreatedAt)      // 新發布優先
          .ToListAsync();
    }

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

        // 【新增】檢查使用者是否已有進行中的委託任務
        bool hasOngoing = await _context.EntrustMissionProgress
            .AnyAsync(p => p.UserId == userId && p.Status == "ongoing");

        if (hasOngoing)
        {
          return new ResultDTO
          {
            Ok = false,
            Code = 403,
            Message = "您目前有進行中的委託任務，請先完成或放棄後才能接取新任務"
          };
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

        return new ResultDTO
        {
          Ok = true,
          Code = 200,
          Message = "成功接取委託任務",
          Data = new
          {
            EntrustMissionId = entrustMissionId,
            Status = mission.Status,
            AcceptedAt = progress.AcceptedAt
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
                // 1️⃣ 獲取最新餘額並鎖定該行 (防止併發超扣)
                // 注意：這裡使用 SQL Raw 來確保 UPDLOCK 生效
                var lastLog = await _context.PointLog
                    .FromSqlRaw("SELECT TOP 1 * FROM point_log WITH (UPDLOCK) WHERE user_id = {0} ORDER BY created_at DESC", userId)
                    .FirstOrDefaultAsync();

                int currentBalance = lastLog?.BalanceAfter ?? 0;

                if (currentBalance < dto.RewardPoints)
                    return new ResultDTO { Ok = false, Code = 400, Message = "點數不足" };

                // 1️⃣ 建立委託任務
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
                await _context.SaveChangesAsync();

                // 2️⃣ 預扣點數
                var reserve = new EntrustMissionPointReserve
                {
                    UserId = userId,
                    EntrustMissionId = entrustMission.EntrustMissionId,
                    ReservedPoints = dto.RewardPoints,
                    Status = "reserved",
                    CreatedAt = now
                };

                // 3️⃣ 點數異動紀錄
                var log = new PointLog
                {
                    UserId = userId,
                    SourceType = "entrust_mission_point_reserve",   // ← 正確的類型！
                    SourceId = entrustMission.EntrustMissionId,
                    PointTypeDefinitionId = (int)PointType.ExpenseEntrustMissionPointReserve,
                    ChangeAmount = -dto.RewardPoints,
                    BalanceAfter = currentBalance - dto.RewardPoints,
                    Status = "success",   // 建議改成 "success"，因為這是正常的預扣，不是 system 調整
                    CreatedAt = now,
                    DisplaySource = $"[預扣] {dto.Title ?? "委託任務"}",
                    DisplaySourceSubTitle = $"委託 #{entrustMission.EntrustMissionId}"
                };
                _context.EntrustMissionPointReserve.Add(reserve);
                _context.PointLog.Add(log);

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
                // 取得真正的錯誤原因
                var realMessage = ex.InnerException?.Message ?? ex.Message;
                return new ResultDTO
                {
                    Ok = false,
                    Code = 500,
                    Message = $"除錯訊息: {realMessage}" // 這會告訴你是 SQL 語法錯、欄位太長、還是外鍵報錯
                };
            }
        }


        public async Task<ResultDTO> UpdateEntrustMissionAsync(int id, EntrustMissionDTO dto)
        {
      var em = await _context.EntrustMission.FindAsync(id);
      if (em == null)
        return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

      // 只允許修改特定欄位
      em.Detail = dto.Detail ?? em.Detail;
      em.RewardPoints = dto.RewardPoints;
      em.Category = dto.Category ?? em.Category;
      em.Deadline = dto.Deadline ?? em.Deadline;
      em.IsUrgent = dto.IsUrgent;
      // em.Status = dto.Status ?? em.Status;  ← 建議不要讓前端隨意改狀態，可移除或嚴格限制
      em.UpdatedAt = DateTime.Now;

      await _context.SaveChangesAsync();
      return new ResultDTO { Ok = true, Code = 204, Message = "更新成功" };
        }

    public async Task<ResultDTO> DeleteEntrustMissionAsync(int id)
    {
      var em = await _context.EntrustMission.FindAsync(id);
      if (em == null)
      {
        return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };
      }

      // 建議改成軟刪除（更安全）
      em.IsActive = false;
      em.UpdatedAt = DateTime.Now;

      //_context.EntrustMission.Remove(em);  // 如果堅持硬刪除
      await _context.SaveChangesAsync();
      return new ResultDTO { Ok = true, Code = 204, Message = "委託任務已下架" };
    }
  }
}
