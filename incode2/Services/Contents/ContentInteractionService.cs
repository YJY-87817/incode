using incode2.Constants;
using incode2.Dtos;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Contents
{
  public class ContentInteractionService : IContentInteractionService
  {
    private readonly incodedatabaseContext _context;
    private readonly IPointLogService _pointLogService;
    private readonly IPointReserveService _pointReserveService;

    public ContentInteractionService(incodedatabaseContext context, IPointLogService pointLogService, IPointReserveService pointReserveService)
        {
            _context = context;
            _pointLogService = pointLogService;
            _pointReserveService = pointReserveService;
        }

        // 取得特定內容的所有互動

        public async Task<List<ContentInteractionDto>> GetContentInteractions(int contentId)
    {
      return await _context.ContentInteraction
        .Where(ci => ci.ContentId == contentId)
        .Include(ci => ci.Category)
        .Select(ci => new ContentInteractionDto
        {
          ContentInteractionId = ci.ContentInteractionId,
          InteractionCategoryId = ci.CategoryId,
          InteractionCategoryName = ci.Category.Name,
          ContentId = ci.ContentId,
          UserId = ci.UserId,
          CreatedAt = ci.CreatedAt ?? DateTime.Now
        })
        .ToListAsync();
    }

    // 取得當前使用者對特定內容的互動類型
    public async Task<List<ContentInteractionDto>> GetUserInteraction(int contentId, int userId)
    {
      return await _context.ContentInteraction
        .Where(ci => ci.ContentId == contentId && ci.UserId == userId)
        .Include(ci => ci.Category)
        .Select(ci => new ContentInteractionDto
        {
          ContentInteractionId = ci.ContentInteractionId,
          InteractionCategoryId = ci.CategoryId,
          InteractionCategoryName = ci.Category.Name,
          ContentId = ci.ContentId,
          UserId = ci.UserId,
          CreatedAt = ci.CreatedAt ?? DateTime.Now
        })
        .ToListAsync();
    }


    // 新增或移除互動（Toggle）
    // 如果使用者已經有該互動 -> 刪除（取消）
    // 如果使用者沒有該互動 -> 新增

    public async Task<ContentInteractionDto> AddOrUpdateInteraction(CreateContentInteractionDto dto)
    {
      // 檢查content是否存在
      var contentExists = await _context.Content.AnyAsync(c => c.ContentId == dto.ContentId);
      if (!contentExists)
      {
        throw new Exception($"內容 ID {dto.ContentId} 不存在");
      }

      // 檢查互動分類是否存在
      var categoryExists = await _context.InteractionCategory
        .AnyAsync(ic => ic.InteractionCategoryId == dto.InteractionCategoryId);
      if (!categoryExists)
        throw new Exception($"互動類別 ID {dto.InteractionCategoryId} 不存在");

      // 查找現有互動（同一使用者對同一內容同一分類）
      var existingInteraction = await _context.ContentInteraction
        .FirstOrDefaultAsync(ci => ci.ContentId == dto.ContentId
          && ci.UserId == dto.UserId
          && ci.CategoryId == dto.InteractionCategoryId);

      if (existingInteraction != null)
      {
        _context.ContentInteraction.Remove(existingInteraction);
        await _context.SaveChangesAsync();

        return null;
      }

      var newInteraction = new ContentInteraction
      {
        ContentId = dto.ContentId,
        UserId = dto.UserId,
        CategoryId = dto.InteractionCategoryId,
        CreatedAt = DateTime.Now
      };
      _context.ContentInteraction.Add(newInteraction);
      await _context.SaveChangesAsync();

      return await _context.ContentInteraction
        .Where(ci => ci.ContentInteractionId == newInteraction.ContentInteractionId)
        .Include(ci => ci.Category)
        .Select(ci => new ContentInteractionDto
        {
          ContentInteractionId = ci.ContentInteractionId,
          InteractionCategoryId = ci.CategoryId,
          InteractionCategoryName = ci.Category.Name,
          ContentId = ci.ContentId,
          UserId = ci.UserId,
          CreatedAt = ci.CreatedAt ?? DateTime.Now
        })
        .FirstAsync();
    }


    // 移除互動

    public async Task<bool> RemoveInteraction(int contentId, int userId, int interactionCategoryId)
    {
      var interaction = await _context.ContentInteraction
        .FirstOrDefaultAsync(ci => ci.ContentId == contentId 
          && ci.UserId == userId 
          && ci.CategoryId == interactionCategoryId);

      if (interaction == null) return false;

      _context.ContentInteraction.Remove(interaction);
      await _context.SaveChangesAsync();
      return true;
    }


    // 取得互動分類列表

    public async Task<List<InteractionCategory>> GetInteractionCategory()
    {
      return await _context.InteractionCategory.ToListAsync();
    }


    // 統計內容的互動數（按分類）

    public async Task<object> GetInteractionStats(int contentId)
    {
      var stats = await _context.ContentInteraction
        .Where(ci => ci.ContentId == contentId)
        .GroupBy(ci => ci.Category.Name)
        .Select(g => new
        {
          InteractionType = g.Key,
          Count = g.Count()
        })
        .ToListAsync();

      return new
      {
        ContentId = contentId,
        InteractionStats = stats,
        TotalInteractions = stats.Sum(s => s.Count)
      };
    }

    public async Task<Dictionary<int, HashSet<int>>> GetUserInteractionLookupAsync(IEnumerable<int> contentIds, int? userId)
    {
      if (!userId.HasValue || userId.Value <= 0)
      {
        return new Dictionary<int, HashSet<int>>();
      }

      var contentIdList = contentIds?.Distinct().ToList() ?? new List<int>();
      if (contentIdList.Count == 0)
      {
        return new Dictionary<int, HashSet<int>>();
      }

      var interactions = await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.UserId == userId.Value && contentIdList.Contains(ci.ContentId))
        .Select(ci => new { ci.ContentId, ci.CategoryId })
        .ToListAsync();

      return interactions
        .GroupBy(ci => ci.ContentId)
        .ToDictionary(g => g.Key, g => g.Select(ci => ci.CategoryId).ToHashSet());
    }

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> contentIds)
    {
      var contentIdList = contentIds?.Distinct().ToList() ?? new List<int>();
      if (contentIdList.Count == 0)
      {
        return new Dictionary<int, int>();
      }

      return await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.CategoryId == 1 && contentIdList.Contains(ci.ContentId))
        .GroupBy(ci => ci.ContentId)
        .Select(g => new { ContentId = g.Key, LikeCount = g.Count() })
        .ToDictionaryAsync(x => x.ContentId, x => x.LikeCount);
    }

    public async Task<int> GetLikeCountAsync(int contentId)
    {
      return await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.ContentId == contentId && ci.CategoryId == 1)
        .CountAsync();
    }

    public async Task<ResultDTO> SelectBestAnswerAsync(int entrustMissionId, int contentId, int selectorUserId)
    {
      if (entrustMissionId <= 0 || contentId <= 0)
      {
        return new ResultDTO { Ok = false, Code = 400, Message = "EntrustMissionId 或 ContentId 必須大於 0" };
      }

      // 最佳解答流程包含任務狀態與點數更新，使用交易確保一致性
      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        var mission = await _context.EntrustMission
          .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId && em.IsActive);

        if (mission == null)
        {
          return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };
        }

        if (mission.UserId != selectorUserId)
        {
          return new ResultDTO { Ok = false, Code = 403, Message = "只有委託任務發起者可以選擇最佳解答" };
        }

        var targetContent = await _context.Content
          .FirstOrDefaultAsync(c => c.ContentId == contentId && c.IsActive == 1);

        if (targetContent == null)
        {
          return new ResultDTO { Ok = false, Code = 404, Message = "回覆內容不存在" };
        }

        // 禁止委託人自己領取自己的委託獎勵
        if (targetContent.UserId == selectorUserId)
        {
           return new ResultDTO
           {
             Ok = false,
             Code = 403,
             Message = "不能選擇自己發表的回覆作為最佳解答"
           };
        }

                var rootContentId = await GetRootContentIdAsync(contentId);
        if (rootContentId == 0 || rootContentId != mission.ContentId)
        {
          return new ResultDTO { Ok = false, Code = 400, Message = "指定內容不屬於此委託任務的貼文" };
        }

        var bestCategoryId = await GetBestAnswerCategoryIdAsync();
        if (bestCategoryId == 0)
        {
          return new ResultDTO { Ok = false, Code = 500, Message = "尚未設定最佳解答互動分類" };
        }

        var bestDescription = $"entrust_mission:{entrustMissionId}";
        var existingBest = await _context.ContentInteraction
          .AnyAsync(ci => ci.CategoryId == bestCategoryId
            && (ci.Description == bestDescription || ci.ContentId == contentId));

        if (existingBest)
        {
          return new ResultDTO { Ok = false, Code = 409, Message = "最佳解答已經設定過" };
        }

        //qiu 扣除發布委託user的預扣點數
        var consumeSuccess = await _pointReserveService.ConsumeAsync(
          mission.UserId,
          mission.EntrustMissionId);

        if (!consumeSuccess)
          {
            await transaction.RollbackAsync();
            return new ResultDTO { Ok = false, Code = 400, Message = "預扣狀態錯誤或不存在" };
        }

        //qiu 發放點數給最佳解答使用者
        var awardSuccess = await _pointLogService.ChangePointsAsync(
          targetContent.UserId,
          mission.RewardPoints,
          "entrust_mission",
          mission.EntrustMissionId,
          PointType.IncomeEntrustMission
          );

        if (!awardSuccess)
        {
           await transaction.RollbackAsync();
           return new ResultDTO { Ok = false, Code = 400, Message = "點數發放失敗" };
        }

        //新增：更新任務狀態為關閉
        mission.Status = "completed"; 
        mission.IsActive = true;  // 想在列表看到，可以設為 true
        mission.UpdatedAt = DateTime.Now;

        _context.ContentInteraction.Add(new ContentInteraction
        {
          ContentId = contentId,
          UserId = selectorUserId,
          CategoryId = bestCategoryId,
          Description = bestDescription,
          CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ResultDTO
        {
          Ok = true,
          Code = 200,
          Message = "最佳解答已選定",
          Data = new
          {
            EntrustMissionId = entrustMissionId,
            BestAnswerContentId = contentId,
            RewardPoints = mission.RewardPoints,
            RewardedUserId = targetContent.UserId
          }
        };
      }
      catch (Exception ex)
      {
        await transaction.RollbackAsync();
        return new ResultDTO { Ok = false, Code = 500, Message = ex.Message };
      }
    }

    private async Task<int> GetRootContentIdAsync(int contentId)
    {
      var currentId = contentId;

      while (true)
      {
        var contentInfo = await _context.Content
          .AsNoTracking()
          .Where(c => c.ContentId == currentId)
          .Select(c => new { c.ContentId, c.ParentContentId })
          .FirstOrDefaultAsync();

        if (contentInfo == null)
        {
          return 0;
        }

        if (!contentInfo.ParentContentId.HasValue)
        {
          return contentInfo.ContentId;
        }

        currentId = contentInfo.ParentContentId.Value;
      }
    }

    private async Task<int> GetBestAnswerCategoryIdAsync()
    {
      var candidateNames = new[] { "best_answer", "best", "accepted", "最佳解答", "最佳解" };

      return await _context.InteractionCategory
        .AsNoTracking()
        .Where(ic => ic.IsActive == 1 && candidateNames.Contains(ic.Name))
        .Select(ic => ic.InteractionCategoryId)
        .FirstOrDefaultAsync();
    }

    public async Task<int?> GetBestAnswerContentId(int entrustMissionId)
    {
      var bestCategoryId = await GetBestAnswerCategoryIdAsync();
      if (bestCategoryId == 0)
      {
        return null;
      }

      var bestDescription = $"entrust_mission:{entrustMissionId}";
      var bestAnswer = await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.CategoryId == bestCategoryId && ci.Description == bestDescription)
        .Select(ci => ci.ContentId)
        .FirstOrDefaultAsync();

      return bestAnswer == 0 ? null : bestAnswer;
    }

    public async Task<List<ContentInteractionDto>> GetInteractions(
      int? contentId,
      int? userId,
      int? interactionCategoryId,
      int? entrustMissionId)
    {
      var query = _context.ContentInteraction
        .AsNoTracking()
        .Include(ci => ci.Category)
        .AsQueryable();

      if (contentId.HasValue)
      {
        query = query.Where(ci => ci.ContentId == contentId.Value);
      }

      if (userId.HasValue)
      {
        query = query.Where(ci => ci.UserId == userId.Value);
      }

      if (interactionCategoryId.HasValue)
      {
        query = query.Where(ci => ci.CategoryId == interactionCategoryId.Value);
      }

      if (entrustMissionId.HasValue)
      {
        var bestDescription = $"entrust_mission:{entrustMissionId.Value}";
        query = query.Where(ci => ci.Description == bestDescription);
      }

      return await query
        .Select(ci => new ContentInteractionDto
        {
          ContentInteractionId = ci.ContentInteractionId,
          InteractionCategoryId = ci.CategoryId,
          InteractionCategoryName = ci.Category.Name,
          ContentId = ci.ContentId,
          UserId = ci.UserId,
          CreatedAt = ci.CreatedAt ?? DateTime.Now
        })
        .ToListAsync();
    }
  }
}
