using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Point
{
  public class EntrustMissionService : IEntrustMissionService
  {
    private readonly incodedatabaseContext _context;
    public EntrustMissionService(incodedatabaseContext context) => _context = context;

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

        if (mission.Deadline.HasValue && mission.Deadline.Value < DateTime.UtcNow)
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
          AcceptedAt = DateTime.UtcNow
        };

        _context.EntrustMissionProgress.Add(progress);

        // 更新主表
        mission.Status = "in_progress";
        mission.UpdatedAt = DateTime.UtcNow;

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
      // 驗證業務邏輯
      if (dto.RewardPoints <= 0)
        return new ResultDTO { Ok = false, Code = 400, Message = "賞金必須大於 0" };

      if (dto.ContentId <= 0)
        return new ResultDTO { Ok = false, Code = 400, Message = "必須選擇關聯內容" };

      if (dto.Deadline.HasValue && dto.Deadline.Value < DateTime.UtcNow)
        return new ResultDTO { Ok = false, Code = 400, Message = "截止時間不能是過去時間" };

      // 驗證 ContentId 是否存在
      var content = await _context.Content
          .AsNoTracking()
          .FirstOrDefaultAsync(c => c.ContentId == dto.ContentId);

      if (content == null)
        return new ResultDTO { Ok = false, Code = 404, Message = "指定的內容不存在" };

      // 驗證發起人是否存在
      var user = await _context.User
          .AsNoTracking()
          .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

      if (user == null)
        return new ResultDTO { Ok = false, Code = 404, Message = "發起人不存在" };

      // 建立新任務記錄
      var em = new EntrustMission
      {
        UserId = dto.UserId,
        ContentId = dto.ContentId,
        Title = dto.Title,  // 使用前端傳入的標題
        Detail = dto.Detail ?? "無詳細說明",
        RewardPoints = dto.RewardPoints,
        Category = dto.Category ?? "其他",
        Deadline = dto.Deadline,
        Status = "open",
        IsUrgent = dto.IsUrgent,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      try
      {
        _context.EntrustMission.Add(em);
        await _context.SaveChangesAsync();

        return new ResultDTO
        {
          Ok = true,
          Code = 201,
          Message = "委託任務建立成功",
          Data = new { EntrustMissionId = em.EntrustMissionId, Title = em.Title }
        };
      }
      catch (DbUpdateException ex)
      {
        // _logger.LogError(ex, "建立委託任務失敗 - UserId: {UserId}", dto.UserId);
        return new ResultDTO { Ok = false, Code = 500, Message = "建立任務失敗，請稍後再試" };
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
      em.UpdatedAt = DateTime.UtcNow;

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
      em.UpdatedAt = DateTime.UtcNow;

      //_context.EntrustMission.Remove(em);  // 如果堅持硬刪除
      await _context.SaveChangesAsync();
      return new ResultDTO { Ok = true, Code = 204, Message = "委託任務已下架" };
    }
  }
}
