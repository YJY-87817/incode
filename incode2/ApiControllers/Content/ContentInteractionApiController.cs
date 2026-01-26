using System.Linq;
using incode2.Dtos;
using incode2.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using incode2.Services.Contents;

namespace incode2.ApiControllers.Content
{
  // ContentInteraction API

  // 基礎 URI: https://localhost:7190/api/contentinteractionapi
  // 所有端點都以此基礎 URI 開頭
  [ApiController]
  [Route("api/[controller]")]
  public class ContentInteractionApiController : ControllerBase
  {
    private readonly IContentInteractionService _contentInteractionService;

    public ContentInteractionApiController(IContentInteractionService contentInteractionService)
    {
      _contentInteractionService = contentInteractionService;
    }


    // 取得特定內容的所有互動

    // URI: GET /api/contentinteractionapi/content/{contentId}
    // 範例:
    // GET https://localhost:7190/api/contentinteractionapi/content/1
    [HttpGet("content/{contentId}")]
    public async Task<IActionResult> GetContentInteractions(int contentId)
    {
      var interactions = await _contentInteractionService.GetContentInteractions(contentId);
      return Ok(new
      {
        ContentId = contentId,
        Interactions = interactions,
        interactions.Count
      });
    }

    // 取得使用者對特定內容的互動
    // URI: GET /api/contentinteractionapi/user/{contentId}?userId={userId}
    // 範例:
    // GET https://localhost:7190/api/contentinteractionapi/user/1?userId=10
    // </summary>
    [HttpGet("user/{contentId}")]
    public async Task<IActionResult> GetUserInteraction(int contentId, [FromQuery] int userId)
    {
      if (userId <= 0) return BadRequest(new { message = "UserId 必須大於 0" });

      var interactions = await _contentInteractionService.GetUserInteraction(contentId, userId);
      if (interactions == null || interactions.Count == 0)
        return Ok(new { message = "此使用者尚未對該內容進行互動", interactions = Array.Empty<object>() });

      return Ok(new
      {
        ContentId = contentId,
        UserId = userId,
        Interactions = interactions,
        interactions.Count
      });
    }


    // 新增或移除互動（Toggle）
    // 同一個 content + user + category 按一次新增，再按一次移除
    // 不同互動類別彼此不互斥，可同時存在
    // URI: POST /api/contentinteractionapi

    // Request Body:
    // {
    //   "interactionCategoryId": 1,  // like
    //   "contentId": 5
    // }

    // 範例:
    // POST https://localhost:7190/api/contentinteractionapi
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddOrUpdateInteraction([FromBody] CreateContentInteractionDto dto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      // 從Claims 中取得當前使用者 ID
      var userId = User.GetId();
      if (userId == 0)
        return Unauthorized(new { message = "無法取得使用者ID" });

      // 驗證 ContentId 和 InteractionCategoryId
      if (dto.ContentId <= 0 || dto.InteractionCategoryId <= 0)
        return BadRequest(new { message = "ContentId 和 InteractionCategoryId 必須大於 0" });

      try
      {
        // 設定 userId，確保使用者只能互動自己的操作
        dto.UserId = userId;

        var result = await _contentInteractionService.AddOrUpdateInteraction(dto);

        // 回傳新增互動或是移除互動
        if (result == null)
        {
          return Ok(new
          {
            message = "互動已移除",
            isRemoved = true,
            data = (object)null
          });
        }
        else
        {
          return Ok(new
          {
            message = "互動已新增或更新",
            isRemoved = false,
            data = result
          });
        }
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { message = ex.Message });
      }
    }


    // 移除互動
    // URI: DELETE /api/contentinteractionapi/{contentId}/{userId}/{interactionCategoryId}
    // 範例:
    // DELETE https://localhost:7190/api/contentinteractionapi/5/10/1

    [HttpDelete("{contentId}/{userId}/{interactionCategoryId}")]
    public async Task<IActionResult> RemoveInteraction(int contentId, int userId, int interactionCategoryId)
    {
      if (contentId <= 0 || userId <= 0 || interactionCategoryId <= 0)
        return BadRequest(new { message = "所有 ID 參數必須大於 0" });

      var success = await _contentInteractionService.RemoveInteraction(contentId, userId, interactionCategoryId);
      if (!success) return NotFound(new { message = "找不到此互動記錄" });

      return Ok(new { message = "互動已移除" });
    }

    // 取得互動分類列表
    // URI: GET /api/contentinteractionapi/categories
    // 範例:
    // GET https://localhost:7190/api/contentinteractionapi/category

    [HttpGet("category")]
    public async Task<IActionResult> GetInteractionCategory()
    {
      var categories = await _contentInteractionService.GetInteractionCategory();
      return Ok(categories);
    }


    // 統計內容的互動數（按分類）
    // URI: GET /api/contentinteractionapi/stats/{contentId}
    // 範例:
    // GET https://localhost:7190/api/contentinteractionapi/stats/1
    [HttpGet("stats/{contentId}")]
    public async Task<IActionResult> GetInteractionStats(int contentId)
    {
      if (contentId <= 0) return BadRequest(new { message = "ContentId 必須大於 0" });

      var stats = await _contentInteractionService.GetInteractionStats(contentId);
      return Ok(stats);
    }
  }
}
