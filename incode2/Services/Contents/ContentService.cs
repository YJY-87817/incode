using incode2.Dtos;
using incode2.Models;
using incode2.Partials;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Contents
{
  public class ContentService : IContentService
  {
    private readonly incodedatabaseContext _context;

    public ContentService(incodedatabaseContext context)
    {
      _context = context;
    }

    public async Task<object> GetContents(int? categoryId, int page, int pageSize)
    {
      var query = _context.Content
      .Include(c => c.User)
      .Include(c => c.Category)
      .Where(c => c.IsActive != 4)
      .OrderByDescending(c => c.CreatedAt)
      .AsQueryable();

      if (categoryId.HasValue)
      {
        query = query.Where(c => c.CategoryId == categoryId.Value);
      }

      var totalCount = await query.CountAsync();
      var items = await query
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(c => new ContentDto
      {
        ContentId = c.ContentId,
        Title = c.Title,
        Body = c.Body,
        CreatedAt = (DateTime)c.CreatedAt,
        Username = c.User.Username,
        CategoryName = c.Category.CategoryName
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

    public async Task<ContentDto?> GetContent(int id)
    {
      return await _context.Content
      .Include(c => c.User)
      .Include(c => c.Category)
      .Where(c => c.ContentId == id && c.IsActive != 4)
      .Select(c => new ContentDto
      {
        ContentId = c.ContentId,
        ParentContentId = c.ParentContentId,
        Title = c.Title,
        Body = c.Body,
        CreatedAt = (DateTime)c.CreatedAt,
        UpdatedAt = (DateTime)c.UpdatedAt,
        Username = c.User.Username,
        CategoryName = c.Category.CategoryName,
        UserId = c.UserId
      })
      .FirstOrDefaultAsync();
    }

    //創立貼文
    public async Task<Content> CreateContent(CreateContentDto dto)
    {
      var content = new Content
      {
        UserId = dto.UserId,
        CategoryId = dto.CategoryId,
        ParentContentId = dto.ParentContentId,
        Title = dto.Title,
        Body = dto.Body,
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

      content.Title = dto.Title;
      content.Body = dto.Body;
      content.UpdatedAt = DateTime.Now;

      await _context.SaveChangesAsync();

      return new UpdateContentDto
      {
        Title = content.Title,
        Body = content.Body
      };
    }

    public async Task<bool> DeleteContent(int id)
    {
      var content = await _context.Content.FindAsync(id);
      if (content == null) return false;

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
      .Where(c => c.ParentContentId == contentId && c.IsActive != 4)
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
        CreatedAt = (DateTime)c.CreatedAt,
        UpdatedAt = (DateTime)c.UpdatedAt,
        Ishidden = c.IsActive == 2 ? true : false
      })
      .ToListAsync();
    }
  }
}
