// Controllers/ContentController.cs
using Microsoft.AspNetCore.Mvc;
using incode2.Models;
using incode2.Data;
using Microsoft.EntityFrameworkCore;

namespace incode2.ApiController
{
  [ApiController]
  [Route("api/[controller]")]
  public class ContentApiController : ControllerBase
  {

    private readonly incodedatabaseContext _context;

    public ContentApiController(incodedatabaseContext context)
    {
      _context = context;
    }

    // GET: api/ContentApi
    // https://localhost:7190/api/ContentApi
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentDto>>> GetContents(
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
      try
      {
        var query = _context.Content
            .Include(c => c.User)
            .Include(c => c.Category)
            .Where(c => (bool)!c.Ishidden)
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

        return Ok(new
        {
          items,
          totalCount,
          page,
          pageSize,
          totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { message = "伺服器錯誤", error = ex.Message });
      }
    }

    // GET: api/Content/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ContentDetailDto>> GetContent(int id)
    {
      var content = await _context.Content
          .Include(c => c.User)
          .Include(c => c.Category)
          .Where(c => c.ContentId == id && c.Ishidden == false)
          .Select(c => new ContentDetailDto
          {
            ContentId = c.ContentId,
            Title = c.Title,
            Body = c.Body,
            CreatedAt = (DateTime)c.CreatedAt,
            UpdatedAt = (DateTime)c.UpdatedAt,
            Username = c.User.Username,
            CategoryName = c.Category.CategoryName,
            UserId = c.UserId
          })
          .FirstOrDefaultAsync();

      if (content == null)
      {
        return NotFound(new { message = "找不到此內容" });
      }

      return Ok(content);
    }

    // POST: api/Content
    [HttpPost]
    public async Task<ActionResult<Content>> CreateContent([FromBody] CreateContentDto dto)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      var content = new Content
      {
        UserId = dto.UserId,
        CategoryId = dto.CategoryId,
        Title = dto.Title,
        Body = dto.Body,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        Ishidden = false
      };

      _context.Content.Add(content);
      await _context.SaveChangesAsync();

      return CreatedAtAction(nameof(GetContent), new { id = content.ContentId }, content);
    }

    // PUT: api/Content/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContent(int id, [FromBody] UpdateContentDto dto)
    {
      var content = await _context.Content.FindAsync(id);

      if (content == null)
      {
        return NotFound(new { message = "找不到此內容" });
      }

      // 儲存舊資料到歷史紀錄
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

      // 更新內容
      content.Title = dto.Title;
      content.Body = dto.Body;
      content.UpdatedAt = DateTime.Now;

      await _context.SaveChangesAsync();

      return Ok(new { message = "更新成功", content });
    }

    // DELETE: api/Content/5 (軟刪除)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
      var content = await _context.Content.FindAsync(id);

      if (content == null)
      {
        return NotFound(new { message = "找不到此內容" });
      }

      content.Ishidden = true;
      content.UpdatedAt = DateTime.Now;
      await _context.SaveChangesAsync();

      return Ok(new { message = "刪除成功" });
    }
  }

  // DTOs
  public class ContentDto
  {
    public int ContentId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Username { get; set; }
    public string CategoryName { get; set; }
  }

  public class ContentDetailDto : ContentDto
  {
    public DateTime? UpdatedAt { get; set; }
    public int UserId { get; set; }
  }

  public class CreateContentDto
  {
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
  }

  public class UpdateContentDto
  {
    public string Title { get; set; }
    public string Body { get; set; }
  }
}