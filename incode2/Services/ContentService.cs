using incode2.Data;
using incode2.Dtos;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services
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
      .Where(c => c.IsActive = false)
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
      .Where(c => c.ContentId == id && c.Ishidden == false)
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

    public async Task<Content> CreateContent(CreateContentDto dto)
    {
      var content = new Content
      {
        UserId = dto.UserId,
        CategoryId = dto.CategoryId,
        ParentContentId = dto.ParentContentId,
        Title = dto.Title,
        Body = dto.Body,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        Ishidden = false
      };

      _context.Content.Add(content);
      await _context.SaveChangesAsync();
      return content;
    }

    public async Task<UpdateContentDto?> UpdateContent(int id, UpdateContentDto dto)
    {
      var content = await _context.Content.FindAsync(id);
      if (content == null) return null;

      var history = new ContentHistory
      {
        ContentId = content.ContentId,
        UserId = content.UserId,
        OldTitle = content.Title,
        OldBody = content.Body,
        Ishidden = content.Ishidden,
        EditedAt = DateTime.Now
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

      content.Ishidden = true;
      content.UpdatedAt = DateTime.Now;
      await _context.SaveChangesAsync();
      return true;
    }
  }
}
