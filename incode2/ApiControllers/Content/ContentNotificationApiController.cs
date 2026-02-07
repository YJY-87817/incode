using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.ApiControllers.Content
{
  // ContentNotification API
  // 基礎 URI: https://localhost:7190/api/ContentNotificationApi

  [Route("api/[controller]")]
  [ApiController]
  public class ContentNotificationApiController : ControllerBase
  {
    private readonly incodedatabaseContext _context;

    public ContentNotificationApiController(incodedatabaseContext context)
    {
      _context = context;
    }

    // 取得所有content通知記錄
    // GET https://localhost:7190/api/ContentNotificationApi
    // Response: 返回所有ContentNotification表中的記錄
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentNotification>>> GetContentNotification()
    {
      return await _context.ContentNotification.ToListAsync();
    }

    // 取得單一content通知詳細資訊
    // GET https://localhost:7190/api/ContentNotificationApi/{id}
    // Path Parameters:
    //   - id (int): 通知記錄的ID
    // Response: 返回指定ID的ContentNotification詳細資料
    [HttpGet("{id}")]
    public async Task<ActionResult<ContentNotification>> GetContentNotification(int id)
    {
      var contentNotification = await _context.ContentNotification.FindAsync(id);

      if (contentNotification == null)
      {
        return NotFound();
      }

      return contentNotification;
    }

    // 更新content通知記錄
    // PUT https://localhost:7190/api/ContentNotificationApi/{id}
    // Path Parameters:
    //   - id (int): 要更新的通知記錄ID
    // Request Body: ContentNotification物件（id必須與URL參數相符）
    // Response: 204 NoContent
    [HttpPut("{id}")]
    public async Task<IActionResult> PutContentNotification(int id, ContentNotification contentNotification)
    {
      if (id != contentNotification.ContentNotificationId)
      {
        return BadRequest();
      }

      _context.Entry(contentNotification).State = EntityState.Modified;

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!ContentNotificationExists(id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }

      return NoContent();
    }

    // 新增content通知記錄
    // POST https://localhost:7190/api/ContentNotificationApi
    // Request Body: ContentNotification物件
    // Response: 201 Created，返回新建的ContentNotification詳細資料
    [HttpPost]
    public async Task<ActionResult<ContentNotification>> PostContentNotification(ContentNotification contentNotification)
    {
      _context.ContentNotification.Add(contentNotification);
      await _context.SaveChangesAsync();

      return CreatedAtAction("GetContentNotification", new { id = contentNotification.ContentNotificationId }, contentNotification);
    }

    // 刪除content通知記錄
    // DELETE https://localhost:7190/api/ContentNotificationApi/{id}
    // Path Parameters:
    //   - id (int): 要刪除的通知記錄ID
    // Response: 204 NoContent
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContentNotification(int id)
    {
      var contentNotification = await _context.ContentNotification.FindAsync(id);
      if (contentNotification == null)
      {
        return NotFound();
      }

      _context.ContentNotification.Remove(contentNotification);
      await _context.SaveChangesAsync();

      return NoContent();
    }

    private bool ContentNotificationExists(int id)
    {
      return _context.ContentNotification.Any(e => e.ContentNotificationId == id);
    }
  }
}
