using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using incode2.Models;
using System.ComponentModel.DataAnnotations;

namespace incode2.ApiControllers.Content
{
  [ApiController]
  [Route("api/[controller]")]
  public class CategoryApiController : ControllerBase
  {
    private readonly incodedatabaseContext _context;

    public CategoryApiController(incodedatabaseContext context)
    {
      _context = context;
    }

    // 取得所有分類（包含分類統計信息）
    // GET https://localhost:7190/api/CategoryApi
    // Query Parameters: 無
    // Response: 返回所有分類列表，包含 { id, parentCategoryId, name, description, topicCount, postCount }
    // 說明:
    //   - topicCount: 該分類下的主文數量 (parent_content_id = NULL)
    //   - postCount: 該分類下的所有內容數量 (主文 + 留言)
    //   - 只計算 IsActive != 3 的內容
    // 範例: GET https://localhost:7190/api/CategoryApi
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
      try
      {
        // 1) 先把所有分類撈出來（不含統計數）
        var categories = await _context.ContentCategory
          .AsNoTracking()
          .Select(c => new CategoryDto
          {
            Id = c.ContentCategoryId,
            ParentCategoryId = c.ParentCategoryId,
            Name = c.CategoryName,
            Description = c.Description,
            TopicCount = 0,
            PostCount = 0
          })
          .ToListAsync();

        // 2) 再從 content 表計算每個分類的主題數 / 文章數
        //    content 資料表：
        //   - category_id：對應 ContentCategoryId
        //   - parent_content_id 為 NULL => 視為主文/主題
        //   - ishidden：比照你 ContentApiController 的寫法，只算「沒被隱藏」的
        var counts = await _context.Content
          .AsNoTracking()
          .Where(c => c.IsActive!=3 && c.CategoryId != null)
          .GroupBy(c => c.CategoryId.Value)
          .Select(g => new
          {
            CategoryId = g.Key,
            PostCount = g.Count(),                         // 該分類所有內容（主文+留言）
            TopicCount = g.Count(c => c.ParentContentId == null) // 主文數
          })
          .ToListAsync();

        var dict = counts.ToDictionary(x => x.CategoryId, x => x);

        // 3) 把計算好的數字塞回各個 CategoryDto
        foreach (var cat in categories)
        {
          if (dict.TryGetValue(cat.Id, out var c))
          {
            cat.PostCount = c.PostCount;
            cat.TopicCount = c.TopicCount;
          }
        }

        // 4) 排序後回傳（這裡用 id 排序，你也可以改成 Name）
        return Ok(categories.OrderBy(c => c.Id));
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { message = "伺服器錯誤", error = ex.Message });
      }
    }

    // 新增分類
    // POST https://localhost:7190/api/CategoryApi
    // Request Body: CreateCategoryDto
    // {
    //   "parentCategoryId": null,  // 可選，父分類ID（用於建立分類階層）
    //   "name": "分類名稱",        // 必填，最多100字
    //   "description": "分類描述"  // 可選，最多200字
    // }
    // Response: 201 Created，返回新建的CategoryDto
    // 說明:
    //   - 如果指定了 parentCategoryId，會驗證父分類是否存在
    //   - IsActive 預設設為 1（啟用狀態）
    // 範例: POST https://localhost:7190/api/CategoryApi
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto request)
    {
      if (request.ParentCategoryId.HasValue)
      {
        var parentExists = await _context.ContentCategory
          .AsNoTracking()
          .AnyAsync(c => c.ContentCategoryId == request.ParentCategoryId.Value);

        if (!parentExists)
        {
          return BadRequest(new { message = "父分類不存在" });
        }
      }

      var entity = new ContentCategory
      {
        ParentCategoryId = request.ParentCategoryId,
        CategoryName = request.Name,
        Description = request.Description,
        IsActive = 1
      };

      _context.ContentCategory.Add(entity);
      await _context.SaveChangesAsync();

      var result = new CategoryDto
      {
        Id = entity.ContentCategoryId,
        ParentCategoryId = entity.ParentCategoryId,
        Name = entity.CategoryName,
        Description = entity.Description,
        TopicCount = 0,
        PostCount = 0
      };

      return Ok(result);
    }

    public class CategoryDto
    {
      public int Id { get; set; }
      public int? ParentCategoryId { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }

      // 前端論壇用到的統計
      public int TopicCount { get; set; }
      public int PostCount { get; set; }
    }

    public class CreateCategoryDto
    {
      public int? ParentCategoryId { get; set; }

      [Required]
      [StringLength(100)]
      public string Name { get; set; }

      [StringLength(200)]
      public string Description { get; set; }
    }
  }
}