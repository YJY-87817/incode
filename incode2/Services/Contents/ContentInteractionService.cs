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

    // 取得使用者對特定內容的互動類型

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
        .Include(ci => ci.Category)
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
  }
}
