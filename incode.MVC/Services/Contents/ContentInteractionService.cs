using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
  public class ContentInteractionService : IContentInteractionService
  {
    private readonly incodedatabaseContext _context;
    private readonly IPointLogService _pointLogService;
    private readonly IPointReserveService _pointReserveService;
    private readonly IMissionService _missionService;

        public ContentInteractionService(incodedatabaseContext context, IPointLogService pointLogService, IPointReserveService pointReserveService, IMissionService missionService)
        {
            _context = context;
            _pointLogService = pointLogService;
            _pointReserveService = pointReserveService;
            _missionService = missionService;
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
    // 只接受會員可以自己設定的分類（InteractionCategoryIds.MemberToggleable）
    // 成功時 Data 為新增的互動，取消互動時 Data 為 null

    public async Task<ResultDTO<ContentInteractionDto?>> AddOrUpdateInteraction(CreateContentInteractionDto dto)
    {
      if (!InteractionCategoryIds.MemberToggleable.Contains(dto.InteractionCategoryId))
        return ResultDTO<ContentInteractionDto?>.Failure("不支援的互動類型", 400);

      var contentExists = await _context.Content.AnyAsync(c => c.ContentId == dto.ContentId && c.IsActive == 1);
      if (!contentExists)
        return ResultDTO<ContentInteractionDto?>.Failure("內容不存在或已被移除", 404);

      // 查找現有互動（同一使用者對同一內容同一分類）
      var existingInteraction = await _context.ContentInteraction
        .FirstOrDefaultAsync(ci => ci.ContentId == dto.ContentId
          && ci.UserId == dto.UserId
          && ci.CategoryId == dto.InteractionCategoryId);

      if (existingInteraction != null)
      {
        _context.ContentInteraction.Remove(existingInteraction);
        await _context.SaveChangesAsync();

        return ResultDTO<ContentInteractionDto?>.Success(null, "互動已移除");
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

      // qiu 按讚任務：進度 = 本週期內「目前仍按讚中的不同內容」數（不含自己的內容）。
      // 原本每按一次就 +1，對同一篇反覆取消 / 重按就能完成任務；改為依實際資料計算，同一內容只算一次
      if (dto.InteractionCategoryId == InteractionCategoryIds.Like)
      {
        var userId = dto.UserId;
        await _missionService.SyncMissionCountAsync(userId, "like", (cycleStart, cycleEnd) =>
          _context.ContentInteraction
            .Where(ci => ci.UserId == userId
              && ci.CategoryId == InteractionCategoryIds.Like
              && ci.CreatedAt >= cycleStart && ci.CreatedAt <= cycleEnd
              && ci.Content.UserId != userId)
            .Select(ci => ci.ContentId)
            .Distinct()
            .CountAsync());
      }

      var created = await _context.ContentInteraction
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

      return ResultDTO<ContentInteractionDto?>.Success(created, "互動已新增");
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
        .Where(ci => ci.CategoryId == InteractionCategoryIds.Like && contentIdList.Contains(ci.ContentId))
        .GroupBy(ci => ci.ContentId)
        .Select(g => new { ContentId = g.Key, LikeCount = g.Count() })
        .ToDictionaryAsync(x => x.ContentId, x => x.LikeCount);
    }

    public async Task<int> GetLikeCountAsync(int contentId)
    {
      return await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.ContentId == contentId && ci.CategoryId == InteractionCategoryIds.Like)
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
        // 先鎖定委託人（只有委託人能選最佳解答；取消委託也鎖同一人），
        // 同時送出的選擇 / 取消會排隊，第二個請求會讀到已結案的狀態，不會重複發放獎勵
        await _pointLogService.LockUsersAsync(selectorUserId);

        var mission = await _context.EntrustMission
          .FirstOrDefaultAsync(em => em.EntrustMissionId == entrustMissionId && em.IsActive);

                if (mission == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "委託任務不存在" };

                if (!mission.IsActive)
                    return new ResultDTO { Ok = false, Code = 410, Message = "該委託任務已下架" };

                if (mission.Status != "open")
                    return new ResultDTO { Ok = false, Code = 400, Message = $"任務狀態為 {mission.Status}，無法選擇最佳解答" };

                if (mission.BestContentId.HasValue)
                    return new ResultDTO { Ok = false, Code = 409, Message = "最佳解答已經設定過" };

                if (mission.UserId != selectorUserId)
                    return new ResultDTO { Ok = false, Code = 403, Message = "只有任務發起者可以選擇最佳解答" };

                // 2. 確認該 content 是否屬於這個委託的討論串
                var rootContentId = await GetRootContentIdAsync(contentId);
                if (rootContentId == 0 || rootContentId != mission.ContentId)
                    return new ResultDTO { Ok = false, Code = 400, Message = "指定內容不屬於此委託任務的討論串" };

                // 3. 取得目標回覆內容
                var targetContent = await _context.Content
                    .FirstOrDefaultAsync(c => c.ContentId == contentId && c.IsActive == 1);

                if (targetContent == null)
                    return new ResultDTO { Ok = false, Code = 404, Message = "回覆內容不存在或已被移除" };

                // 4. 禁止委託人選自己的回覆
                if (targetContent.UserId == selectorUserId)
                    return new ResultDTO { Ok = false, Code = 403, Message = "不能選擇自己發表的回覆作為最佳解答" };

                // 5. 消耗預扣點數
                var consumeSuccess = await _pointReserveService.ConsumeAsync(
                    mission.UserId,
                    entrustMissionId
                );

                if (!consumeSuccess)
                {
                    await transaction.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 400, Message = "預扣點數狀態錯誤或已消耗/退還" };
                }

                // 6. 發放點數給最佳解答作者
                string displaySubTitle = $"委託任務 #{entrustMissionId} 最佳解答獎勵";
                var awardSuccess = await _pointLogService.ChangePointsAsync(
                    targetContent.UserId,
                    mission.RewardPoints,
                    "entrust_mission",
                    mission.EntrustMissionId,
                    PointType.IncomeEntrustMission,  // 假設你有定義這個 enum 或常數
                    displaySource: "委託任務最佳解答",
                    displaySubTitle: displaySubTitle
                );

                if (!awardSuccess)
                {
                    await transaction.RollbackAsync();
                    return new ResultDTO { Ok = false, Code = 500, Message = "點數發放失敗" };
                }

                // 7. 更新 entrust_mission 主表
                mission.BestContentId = contentId;
                mission.BestUserId = targetContent.UserId;
                mission.SelectedAt = DateTime.Now;
                mission.Status = "completed";
                mission.UpdatedAt = DateTime.Now;

                // 8. (可選) 記錄「最佳解答」互動分類，方便前端顯示徽章或標記
                var bestCategoryId = await GetBestAnswerCategoryIdAsync();
                if (bestCategoryId > 0)
                {
                    // 避免重複新增
                    bool alreadyHasBest = await _context.ContentInteraction
                        .AnyAsync(ci => ci.ContentId == contentId && ci.CategoryId == bestCategoryId);

                    if (!alreadyHasBest)
                    {
                        _context.ContentInteraction.Add(new ContentInteraction
                        {
                            ContentId = contentId,
                            UserId = selectorUserId,
                            CategoryId = bestCategoryId,
                            Description = $"entrust_mission:{entrustMissionId}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ResultDTO
                {
                    Ok = true,
                    Code = 200,
                    Message = "最佳解答已選定並完成獎勵發放",
                    Data = new
                    {
                        EntrustMissionId = entrustMissionId,
                        BestContentId = contentId,
                        BestUserId = targetContent.UserId,
                        RewardPoints = mission.RewardPoints,
                        SelectedAt = mission.SelectedAt
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ResultDTO { Ok = false, Code = 500, Message = "伺服器錯誤，請稍後再試" };
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
