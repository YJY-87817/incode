using incode2.Dtos;
using incode2.Models;
using incode2.Partials;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace incode2.Services.Contents
{
  public class ContentService : IContentService
  {
    private readonly incodedatabaseContext _context;
    private readonly IWebHostEnvironment _environment;

    public ContentService(incodedatabaseContext context, IWebHostEnvironment environment)
    {
      _context = context;
      _environment = environment;
    }

    public async Task<ContentListResponseDto> GetContents(int? categoryId, int page, int pageSize, int? userId, string? keyword, string? types, string? sortBy)
    {
      var typeFilter = ParseTypes(types);
      if (types != null && typeFilter.Count == 0)
      {
        return new ContentListResponseDto
        {
          Items = new List<ContentListItemDto>(),
          TotalCount = 0,
          TotalPages = 0,
          CurrentPage = page,
          PageSize = pageSize,
          ReplyStatsIncluded = true
        };
      }

      var query = _context.Content
        .AsNoTracking()
        .Where(c => c.IsActive == 1)
        .AsQueryable();

      if (userId.HasValue && userId.Value > 0)
      {
        const int hideCategoryId = 2;
        query = query.Where(c => !_context.ContentInteraction.Any(ci => ci.ContentId == c.ContentId && ci.UserId == userId.Value && ci.CategoryId == hideCategoryId));
      }

      if (categoryId.HasValue)
      {
        query = query.Where(c => c.CategoryId == categoryId.Value);
      }

      if (typeFilter.Count > 0)
      {
        query = query.Where(c => typeFilter.Contains(c.Type));
      }

      if (!string.IsNullOrWhiteSpace(keyword))
      {
        var normalizedKeyword = keyword.Trim();
        query = query.Where(c => c.Title.Contains(normalizedKeyword)
          || (!c.Body.StartsWith("/contents/") && c.Body.Contains(normalizedKeyword)));
      }

      query = string.Equals(sortBy, "latest", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(sortBy)
        ? query.OrderByDescending(c => c.CreatedAt)
        : query.OrderByDescending(c => c.CreatedAt);

      var totalCount = await query.CountAsync();
      var pageItems = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new
        {
          c.ContentId,
          c.ParentContentId,
          c.UserId,
          Username = c.User.Username,
          c.CategoryId,
          CategoryName = c.Category != null ? c.Category.CategoryName : null,
          c.Title,
          c.Body,
          c.Type,
          c.CreatedAt
        })
        .ToListAsync();

      var contentIds = pageItems.Select(c => c.ContentId).ToList();
      var replyStats = new List<(int ContentId, int ReplyCount, DateTime? LastReplyTime, string LastReplyUser, int? LastReplyUserId)>();
      var likeCounts = new Dictionary<int, int>();
      if (contentIds.Count > 0)
      {
        var replyStatsRaw = await _context.Content
          .AsNoTracking()
          .Where(c => c.IsActive == 1
            && c.ParentContentId != null
            && contentIds.Contains(c.ParentContentId.Value)
            && new[] { 2, 3, 4 }.Contains(c.Type))
          .GroupBy(c => c.ParentContentId.Value)
          .Select(g => new
          {
            ContentId = g.Key,
            ReplyCount = g.Count(),
            LastReply = g.OrderByDescending(x => x.CreatedAt)
              .Select(x => new { x.CreatedAt, x.UserId })
              .FirstOrDefault()
          })
          .ToListAsync();

        var lastReplyUserIds = replyStatsRaw
          .Select(x => x.LastReply != null ? (int?)x.LastReply.UserId : null)
          .Where(x => x.HasValue)
          .Select(x => x.Value)
          .Distinct()
          .ToList();

        var userLookup = lastReplyUserIds.Count == 0
          ? new Dictionary<int, string>()
          : await _context.User
            .AsNoTracking()
            .Where(u => lastReplyUserIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Username })
            .ToDictionaryAsync(u => u.UserId, u => u.Username);

        replyStats = replyStatsRaw.Select(x =>
        {
          var lastReplyUser = x.LastReply != null && userLookup.TryGetValue(x.LastReply.UserId, out var username)
            ? username
            : null;

          return new ValueTuple<int, int, DateTime?, string, int?>(
            x.ContentId,
            x.ReplyCount,
            x.LastReply?.CreatedAt,
            lastReplyUser,
            x.LastReply != null ? (int?)x.LastReply.UserId : null);
        }).ToList();

        likeCounts = await _context.ContentInteraction
          .AsNoTracking()
          .Where(ci => ci.CategoryId == 1 && contentIds.Contains(ci.ContentId))
          .GroupBy(ci => ci.ContentId)
          .Select(g => new { ContentId = g.Key, LikeCount = g.Count() })
          .ToDictionaryAsync(x => x.ContentId, x => x.LikeCount);
      }

      var replyLookup = replyStats.ToDictionary(x => x.ContentId, x => x);
      // 將查詢結果轉成 API 回傳用的 ContentListItemDto
      var items = pageItems.Select(c =>
      {
        replyLookup.TryGetValue(c.ContentId, out var stats);
        likeCounts.TryGetValue(c.ContentId, out var likeCount);
        return new ContentListItemDto
        {
          ContentId = c.ContentId,
          ParentContentId = c.ParentContentId,
          UserId = c.UserId,
          Username = c.Username,
          CategoryId = c.CategoryId,
          CategoryName = c.CategoryName,
          Title = c.Title,
          Body = c.Body,
          Type = c.Type,
          CreatedAt = c.CreatedAt,
          ReplyCount = stats.ContentId == 0 ? 0 : stats.ReplyCount,
          LikeCount = likeCount,
          LastReplyTime = stats.ContentId == 0 ? null : stats.LastReplyTime,
          LastReplyUser = stats.ContentId == 0 ? null : stats.LastReplyUser,
          LastReplyUserId = stats.ContentId == 0 ? null : stats.LastReplyUserId,
          PreviewText = BuildPreviewText(c.Title, c.Body, 120),
          PreviewImages = BuildPreviewImages(c.Body, 3),
          PreviewHtml = null
        };
      }).ToList();

      return new ContentListResponseDto
      {
        Items = items,
        TotalCount = totalCount,
        CurrentPage = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        ReplyStatsIncluded = true
      };
    }

    public async Task<ContentDto?> GetContent(int id, int? userId)
    {
      var content = await _context.Content
      .Include(c => c.User)
      .Include(c => c.Category)
      .Where(c => c.ContentId == id && c.IsActive == 1)
      .Select(c => new
      {
        c.ContentId,
        c.ParentContentId,
        c.Title,
        c.Body,
        c.Type,
        c.CreatedAt,
        c.UpdatedAt,
        Username = c.User.Username,
        CategoryName = c.Category.CategoryName,
        c.UserId,
        c.CategoryId
      })
      .FirstOrDefaultAsync();

      if (content == null)
        return null;

      // 計算回覆統計信息
      int replyCount = 0;
      DateTime? lastReplyTime = null;
      string lastReplyUser = null;
      int? lastReplyUserId = null;

      var replyStats = await _context.Content
        .AsNoTracking()
        .Where(c => c.IsActive == 1
          && c.ParentContentId == id
          && new[] { 2, 3, 4 }.Contains(c.Type))
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new { x.CreatedAt, x.UserId })
        .ToListAsync();

      if (replyStats.Count > 0)
      {
        replyCount = replyStats.Count;
        var lastReply = replyStats.First();
        lastReplyTime = lastReply.CreatedAt;
        lastReplyUserId = lastReply.UserId;

        // 取得最後回覆者的用戶名
        var lastReplyUserData = await _context.User
          .AsNoTracking()
          .Where(u => u.UserId == lastReply.UserId)
          .Select(u => u.Username)
          .FirstOrDefaultAsync();
        lastReplyUser = lastReplyUserData;
      }

      // 互動資訊從 ContentInteraction 資料表彙整
      var interactionLookup = await GetUserInteractionLookupAsync(new[] { id }, userId);
      interactionLookup.TryGetValue(id, out var categories);
      var myInteractions = BuildMyInteractions(categories);
      var likeCount = await _context.ContentInteraction
        .AsNoTracking()
        .Where(ci => ci.ContentId == id && ci.CategoryId == 1)
        .CountAsync();

      return new ContentDto
      {
        ContentId = content.ContentId,
        ParentContentId = content.ParentContentId,
        Title = content.Title,
        Body = content.Body,
        Type = content.Type,
        CreatedAt = (DateTime)content.CreatedAt,
        UpdatedAt = (DateTime)content.UpdatedAt,
        Username = content.Username,
        CategoryName = content.CategoryName,
        UserId = content.UserId,
        CategoryId = content.CategoryId,
        ReplyCount = replyCount,
        LikeCount = likeCount,
        LastReplyTime = lastReplyTime,
        LastReplyUser = lastReplyUser,
        LastReplyUserId = lastReplyUserId,
        MyInteractions = myInteractions
      };
    }

    public async Task<PagedResultDto<ContentDto>> GetChildrenByContentId(int contentId, string? types, int page, int pageSize, int? userId)
    {
      var typeFilter = ParseTypes(types);
      if (types != null && typeFilter.Count == 0)
      {
        return new PagedResultDto<ContentDto>
        {
          Items = new List<ContentDto>(),
          TotalCount = 0,
          TotalPages = 0,
          CurrentPage = page,
          PageSize = pageSize
        };
      }

      var query = _context.Content
        .AsNoTracking()
        .Include(c => c.User)
        .Include(c => c.Category)
        .Where(c => c.ParentContentId == contentId && c.IsActive == 1)
        .AsQueryable();

      if (typeFilter.Count > 0)
      {
        query = query.Where(c => typeFilter.Contains(c.Type));
      }

      var totalCount = await query.CountAsync();

      var items = await query
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        // 將子回覆資料投影成 ContentDto
        .Select(c => new ContentDto
        {
          ContentId = c.ContentId,
          ParentContentId = c.ParentContentId,
          UserId = c.UserId,
          Username = c.User.Username,
          CategoryId = c.CategoryId,
          CategoryName = c.Category != null ? c.Category.CategoryName : null,
          Title = c.Title,
          Body = c.Body,
          Type = c.Type,
          CreatedAt = (DateTime)c.CreatedAt,
          UpdatedAt = (DateTime)c.UpdatedAt,
          Ishidden = c.IsActive == 2
        })
        .ToListAsync();

      var contentIds = items.Select(item => item.ContentId).ToList();

      // Calculate like counts for each child content
      var likeCounts = new Dictionary<int, int>();
      if (contentIds.Count > 0)
      {
        likeCounts = await _context.ContentInteraction
          .AsNoTracking()
          .Where(ci => ci.CategoryId == 1 && contentIds.Contains(ci.ContentId))
          .GroupBy(ci => ci.ContentId)
          .Select(g => new { ContentId = g.Key, LikeCount = g.Count() })
          .ToDictionaryAsync(x => x.ContentId, x => x.LikeCount);
      }

      // Populate like counts
      foreach (var item in items)
      {
        likeCounts.TryGetValue(item.ContentId, out var likeCount);
        item.LikeCount = likeCount;
      }

      var interactionLookup = await GetUserInteractionLookupAsync(contentIds, userId);
      foreach (var item in items)
      {
        interactionLookup.TryGetValue(item.ContentId, out var categories);
        item.MyInteractions = BuildMyInteractions(categories);
      }

      return new PagedResultDto<ContentDto>
      {
        Items = items,
        TotalCount = totalCount,
        CurrentPage = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
      };
    }

    //創立貼文
    public async Task<Content> CreateContent(CreateContentDto dto)
    {
      int? categoryId = dto.CategoryId;

      // 如果有父內容 ID（回文/留言/留言回覆），則繼承父內容的 CategoryId
      if (dto.ParentContentId.HasValue)
      {
        var parentContent = await _context.Content
          .Where(c => c.ContentId == dto.ParentContentId.Value)
          .Select(c => new { c.CategoryId })
          .FirstOrDefaultAsync();

        if (parentContent != null)
        {
          categoryId = parentContent.CategoryId;
        }
      }

      // 內文寫入檔案後，Body 僅保存檔案路徑
      var bodyUri = await SaveContentBodyAsync(dto.Body);

      var content = new Content
      {
        UserId = dto.UserId,
        CategoryId = categoryId,
        ParentContentId = dto.ParentContentId,
        Title = dto.Title,
        Body = bodyUri,
        Type = dto.Type,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        IsActive = 1
      };

      _context.Content.Add(content);
      await _context.SaveChangesAsync();
      return content;
    }

    //修改content
    public async Task<UpdateContentDto?> UpdateContent(int id, UpdateContentDto dto, int currentUserId)
    {
      var content = await _context.Content.FindAsync(id);
      if (content == null) return null;

      // 驗證擁有者
      if (content.UserId != currentUserId)
      {
        return null; // 或拋出 Unauthorized
      }

      var history = new ContentHistory
      {
        ContentId = content.ContentId,
        UserId = content.UserId,
        OldTitle = content.Title,
        OldBody = content.Body,
        CreatedAt = DateTime.Now
      };
      _context.ContentHistory.Add(history);

      // 更新內容時同步覆寫檔案內容
      var bodyUri = await SaveContentBodyAsync(dto.Body);

      content.Title = dto.Title;
      content.Body = bodyUri;
      content.UpdatedAt = DateTime.Now;

      await _context.SaveChangesAsync();

      return new UpdateContentDto
      {
        Title = content.Title,
        Body = content.Body,
        UpdatedAt = content.UpdatedAt  // 添加更新時間
      };
    }

    public async Task<bool> DeleteContent(int id, int currentUserId)
    {
      var content = await _context.Content.FindAsync(id);
      if (content == null) return false;

      // 添加權限檢查
      if (content.UserId != currentUserId)
      {
        return false; // 不是作者，無權刪除
      }

      content.IsActive = 2;
      content.UpdatedAt = DateTime.Now;
      await _context.SaveChangesAsync();
      return true;
    }

    public async Task<List<ContentDto>> GetCommentsByContentId(int contentId)
    {
      var result = await GetChildrenByContentId(contentId, null, 1, int.MaxValue, null);
      return result.Items;
    }

    public async Task<object> GetUserContents(int userId, IEnumerable<int>? types, int page, int pageSize)
    {
      var typeList = types?.Distinct().ToList();
      if (types != null && (typeList == null || typeList.Count == 0))
      {
        return new
        {
          items = new List<ContentDto>(),
          totalCount = 0,
          page,
          pageSize,
          totalPages = 0
        };
      }

      var query = _context.Content
        .Include(c => c.User)
        .Include(c => c.Category)
        .Where(c => c.UserId == userId && c.IsActive == 1)
        .OrderByDescending(c => c.CreatedAt)
        .AsQueryable();

      if (typeList != null && typeList.Count > 0)
      {
        query = query.Where(c => typeList.Contains(c.Type));
      }

      var totalCount = await query.CountAsync();

      var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new ContentDto
        {
          ContentId = c.ContentId,
          ParentContentId = c.ParentContentId,
          UserId = c.UserId,
          Username = c.User.Username,
          CategoryId = c.CategoryId,
          CategoryName = c.Category != null ? c.Category.CategoryName : null,
          Title = c.Title,
          Body = c.Body,
          Type = c.Type,
          CreatedAt = (DateTime)c.CreatedAt,
          UpdatedAt = (DateTime)c.UpdatedAt,
          Ishidden = c.IsActive == 2 ? true : false
        })
        .ToListAsync();

      return new
      {
        items,
        totalCount,
        page,
        pageSize,
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
      };
    }

    public async Task<object> GetUserInteractedContents(int userId, int? interactionCategoryId, IEnumerable<string> interactionCategoryNames, int page, int pageSize)
    {
      var resolvedCategoryId = interactionCategoryId;
      if (!resolvedCategoryId.HasValue)
      {
        var normalizedNames = interactionCategoryNames
          .Where(name => !string.IsNullOrWhiteSpace(name))
          .Select(name => name.Trim().ToLowerInvariant())
          .Distinct()
          .ToList();

        if (normalizedNames.Count > 0)
        {
          resolvedCategoryId = await _context.InteractionCategory
            .Where(ic => normalizedNames.Contains(ic.Name.ToLower()))
            .Select(ic => (int?)ic.InteractionCategoryId)
            .FirstOrDefaultAsync();
        }
      }

      if (!resolvedCategoryId.HasValue)
      {
        return new
        {
          items = new List<ContentDto>(),
          totalCount = 0,
          page,
          pageSize,
          totalPages = 0
        };
      }

      var query = _context.Content
        .Include(c => c.User)
        .Include(c => c.Category)
        .Where(c => c.IsActive == 1)
        .Where(c => _context.ContentInteraction.Any(ci => ci.ContentId == c.ContentId && ci.UserId == userId && ci.CategoryId == resolvedCategoryId.Value))
        .OrderByDescending(c => c.CreatedAt);

      var totalCount = await query.CountAsync();

      var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new ContentDto
        {
          ContentId = c.ContentId,
          ParentContentId = c.ParentContentId,
          UserId = c.UserId,
          Username = c.User.Username,
          CategoryId = c.CategoryId,
          CategoryName = c.Category != null ? c.Category.CategoryName : null,
          Title = c.Title,
          Body = c.Body,
          Type = c.Type,
          CreatedAt = (DateTime)c.CreatedAt,
          UpdatedAt = (DateTime)c.UpdatedAt,
          Ishidden = c.IsActive == 2 ? true : false
        })
        .ToListAsync();

      return new
      {
        items,
        totalCount,
        page,
        pageSize,
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
      };
    }

    private async Task<string> SaveContentBodyAsync(string htmlBody)
    {
      var webRootPath = _environment.WebRootPath;
      if (string.IsNullOrWhiteSpace(webRootPath))
      {
        webRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
      }

      var contentsPath = Path.Combine(webRootPath, "contents");
      Directory.CreateDirectory(contentsPath);

      var fileName = $"content-{Guid.NewGuid():N}.html";
      var filePath = Path.Combine(contentsPath, fileName);

      await File.WriteAllTextAsync(filePath, htmlBody ?? string.Empty, new UTF8Encoding(false));

      return $"/contents/{fileName}";
    }

    private static List<int> ParseTypes(string? types)
    {
      if (string.IsNullOrWhiteSpace(types))
      {
        return new List<int>();
      }

      return types
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(value => int.TryParse(value.Trim(), out var parsed) ? (int?)parsed : null)
        .Where(value => value.HasValue)
        .Select(value => value.Value)
        .Distinct()
        .ToList();
    }

    private static bool IsBodyPath(string body)
      => !string.IsNullOrWhiteSpace(body) && body.StartsWith("/contents/", StringComparison.OrdinalIgnoreCase);

    private static string BuildPreviewText(string title, string body, int maxLength)
    {
      if (IsBodyPath(body))
      {
        return title ?? string.Empty;
      }

      var stripped = Regex.Replace(body ?? string.Empty, "<.*?>", string.Empty).Trim();
      if (string.IsNullOrWhiteSpace(stripped))
      {
        return title ?? string.Empty;
      }

      return stripped.Length <= maxLength ? stripped : stripped[..maxLength];
    }

    private static List<string> BuildPreviewImages(string body, int maxImages)
    {
      if (IsBodyPath(body) || string.IsNullOrWhiteSpace(body))
      {
        return new List<string>();
      }

      var matches = Regex.Matches(body, "<img[^>]+src\\s*=\\s*['\"](?<src>[^'\"]+)['\"]", RegexOptions.IgnoreCase);
      return matches
        .Select(match => match.Groups["src"].Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct()
        .Take(maxImages)
        .ToList();
    }

    private async Task<Dictionary<int, HashSet<int>>> GetUserInteractionLookupAsync(IEnumerable<int> contentIds, int? userId)
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

    private static MyInteractionsDto BuildMyInteractions(IReadOnlyCollection<int> categories)
    {
      var categorySet = categories ?? new List<int>();
      var interactions = new MyInteractionsDto
      {
        Like = categorySet.Contains(1),
        Hide = categorySet.Contains(2),
        Bookmark = categorySet.Contains(3),
        Report = categorySet.Contains(4),
        Categories = categorySet.OrderBy(value => value).ToList()
      };

      foreach (var categoryId in categorySet)
      {
        var key = categoryId.ToString();
        if (interactions.Map.ContainsKey(key))
        {
          interactions.Map[key] = true;
        }
      }

      return interactions;
    }
  }
}
