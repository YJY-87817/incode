using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Contents
{
  public class ContentInteractionService : IContentInteractionService
  {
    private readonly incodedatabaseContext _context;

    public ContentInteractionService(incodedatabaseContext context)
    {
      _context = context;
    }

    /// <summary>
    /// 取得特定內容的所有互動
    /// </summary>
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

    /// <summary>
    /// 取得使用者對特定內容的互動類型
    /// </summary>
    public async Task<ContentInteractionDto?> GetUserInteraction(int contentId, int userId)
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
        .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 新增或移除互動（Toggle）
    /// 如果使用者已經有該互動 -> 刪除（取消）
    /// 如果使用者沒有該互動 -> 新增
    /// 如果使用者有其他互動 -> 更新為新的互動類型
    /// </summary>
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
        throw new Exception($"互動分類 ID {dto.InteractionCategoryId} 不存在");

      // 查找現有互動（同一使用者對同一內容）
      var existingInteraction = await _context.ContentInteraction
        .FirstOrDefaultAsync(ci => ci.ContentId == dto.ContentId && ci.UserId == dto.UserId);

      if (existingInteraction != null)
      {
        // 如果已存在相同的互動類型 -> 刪除（取消）
        if (existingInteraction.CategoryId == dto.InteractionCategoryId)
        {
          _context.ContentInteraction.Remove(existingInteraction);
          //實際刪除在這行
          await _context.SaveChangesAsync();
          
          // 回傳 null 表示互動已被移除
          return null;
        }
        else
        {
          // 如果互動類型不同 -> 更新為新的互動類型
          existingInteraction.CategoryId = dto.InteractionCategoryId;
          existingInteraction.CreatedAt = DateTime.Now;
        }
      }
      else
      {
        // 新增互動
        var newInteraction = new ContentInteraction
        {
          ContentId = dto.ContentId,
          UserId = dto.UserId,
          CategoryId = dto.InteractionCategoryId,
          CreatedAt = DateTime.Now
        };
        _context.ContentInteraction.Add(newInteraction);
      }

      await _context.SaveChangesAsync();

      // 回傳更新後的互動
      return await GetUserInteraction(dto.ContentId, dto.UserId);
    }

    /// <summary>
    /// 移除互動
    /// </summary>
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

    /// <summary>
    /// 取得互動分類列表
    /// </summary>
    public async Task<List<InteractionCategory>> GetInteractionCategories()
    {
      return await _context.InteractionCategory.ToListAsync();
    }

    /// <summary>
    /// 統計內容的互動數（按分類）
    /// </summary>
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
        TotalInteractions = stats.Sum(s => (int)s.GetType().GetProperty("Count").GetValue(s))
      };
    }
  }
}
