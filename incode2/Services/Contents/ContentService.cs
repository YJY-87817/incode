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
      }

      var replyLookup = replyStats.ToDictionary(x => x.ContentId, x => x);
      var items = pageItems.Select(c =>
      {
        replyLookup.TryGetValue(c.ContentId, out var stats);
        return new ContentListItemDto
        {
          ContentId = c.ContentId,
          UserId = c.UserId,
          Username = c.Username,
          CategoryId = c.CategoryId,
          CategoryName = c.CategoryName,
          Title = c.Title,
          Body = c.Body,
          Type = c.Type,
          CreatedAt = c.CreatedAt,
          ReplyCount = stats.ContentId == 0 ? 0 : stats.ReplyCount,
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

    public async Task<ContentDto?> GetContent(int id)
    {
      return await _context.Content
      .Include(c => c.User)
      .Include(c => c.Category)
      .Where(c => c.ContentId == id && c.IsActive == 1)
      .Select(c => new ContentDto
      {
        ContentId = c.ContentId,
        ParentContentId = c.ParentContentId,
        Title = c.Title,
        Body = c.Body,
        Type = c.Type,
        CreatedAt = (DateTime)c.CreatedAt,
        UpdatedAt = (DateTime)c.UpdatedAt,
        Username = c.User.Username,
        CategoryName = c.Category.CategoryName,
        UserId = c.UserId
      })
      .FirstOrDefaultAsync();
    }

    public async Task<PagedResultDto<ContentDto>> GetChildrenByContentId(int contentId, string? types, int page, int pageSize)
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
      return await _context.Content
      .Include(c => c.User)
      .Include(c => c.Category)
      .Where(c => c.ParentContentId == contentId && c.IsActive == 1)
      .OrderByDescending(c => c.CreatedAt)
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
    }

    public async Task<object> GetUserContentsByTypes(int userId, IEnumerable<int> types, int page, int pageSize)
    {
      var typeList = types?.Distinct().ToList() ?? new List<int>();
      if (typeList.Count == 0)
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
        .Where(c => c.UserId == userId && c.IsActive == 1 && typeList.Contains(c.Type))
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

    public async Task<List<ContentDto>> GetUserContents(int userId)
    {
      return await _context.Content
        .Include(c => c.User)
        .Include(c => c.Category)
        .Where(c => c.UserId == userId && c.IsActive == 1)
        .OrderByDescending(c => c.CreatedAt)
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
  }
}
