using incode2.Services;
using incode2.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace incode2.ApiController
{
  /// <summary>
  /// ContentInteraction API
  /// 
  /// 基礎 URI: https://localhost:7190/api/contentinteractionapi
  /// 
  /// 所有端點都以此基礎 URI 開頭
  /// </summary>
  [ApiController]
  [Route("api/[controller]")]
  public class ContentInteractionApiController : ControllerBase
  {
    private readonly IContentInteractionService _contentInteractionService;

    public ContentInteractionApiController(IContentInteractionService contentInteractionService)
    {
      _contentInteractionService = contentInteractionService;
    }

    /// <summary>
    /// 取得特定內容的所有互動
    /// 
    /// URI: GET /api/contentinteractionapi/content/{contentId}
    /// 
    /// 範例:
    /// GET https://localhost:7190/api/contentinteractionapi/content/1
    /// </summary>
    [HttpGet("content/{contentId}")]
    public async Task<IActionResult> GetContentInteractions(int contentId)
    {
      var interactions = await _contentInteractionService.GetContentInteractions(contentId);
      return Ok(new
      {
        ContentId = contentId,
        Interactions = interactions,
        Count = interactions.Count
      });
    }

    /// <summary>
    /// 取得使用者對特定內容的互動
    /// 
    /// URI: GET /api/contentinteractionapi/user/{contentId}?userId={userId}
    /// 
    /// 範例:
    /// GET https://localhost:7190/api/contentinteractionapi/user/1?userId=10
    /// </summary>
    [HttpGet("user/{contentId}")]
    public async Task<IActionResult> GetUserInteraction(int contentId, [FromQuery] int userId)
    {
      if (userId <= 0) return BadRequest(new { message = "UserId 必須大於 0" });

      var interaction = await _contentInteractionService.GetUserInteraction(contentId, userId);
      if (interaction == null)
        return Ok(new { message = "此使用者尚未對該內容進行互動" });

      return Ok(interaction);
    }

    /// <summary>
    /// 新增或移除互動（Toggle）
    /// 同一個 content + user + category 按一次新增，再按一次移除
    /// 如果互動類型不同，則更新為新類型
    /// 
    /// URI: POST /api/contentinteractionapi
    /// 
    /// Request Body:
    /// {
    ///   "interactionCategoryId": 1,  // like
    ///   "contentId": 5,
    ///   "userId": 10
    /// }
    /// 
    /// 範例:
    /// POST https://localhost:7190/api/contentinteractionapi
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddOrUpdateInteraction([FromBody] CreateContentInteractionDto dto)
    {
      if (!ModelState.IsValid) { return BadRequest(ModelState); }

      if (dto.ContentId <= 0 || dto.UserId <= 0 || dto.InteractionCategoryId <= 0)
      { return BadRequest(new { message = "ContentId、UserId 和 InteractionCategoryId 必須大於 0" }); }

      try
      {
        var result = await _contentInteractionService.AddOrUpdateInteraction(dto);
        
        if (result == null)
        {
          // 互動已被移除
          return Ok(new
          {
            message = "互動已移除",
            isRemoved = true,
            data = (object)null
          });
        }
        else
        {
          // 互動已被新增或更新
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
        return BadRequest(new { message = ex.Message });
      }
    }

    /// <summary>
    /// 移除互動
    /// 
    /// URI: DELETE /api/contentinteractionapi/{contentId}/{userId}/{interactionCategoryId}
    /// 
    /// 範例:
    /// DELETE https://localhost:7190/api/contentinteractionapi/5/10/1
    /// </summary>
    [HttpDelete("{contentId}/{userId}/{interactionCategoryId}")]
    public async Task<IActionResult> RemoveInteraction(int contentId, int userId, int interactionCategoryId)
    {
      if (contentId <= 0 || userId <= 0 || interactionCategoryId <= 0)
        return BadRequest(new { message = "所有 ID 參數必須大於 0" });

      var success = await _contentInteractionService.RemoveInteraction(contentId, userId, interactionCategoryId);
      if (!success) return NotFound(new { message = "找不到此互動記錄" });

      return Ok(new { message = "互動已移除" });
    }

    /// <summary>
    /// 取得互動分類列表
    /// 
    /// URI: GET /api/contentinteractionapi/categories
    /// 
    /// 範例:
    /// GET https://localhost:7190/api/contentinteractionapi/categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetInteractionCategories()
    {
      var categories = await _contentInteractionService.GetInteractionCategories();
      return Ok(categories);
    }

    /// <summary>
    /// 統計內容的互動數（按分類）
    /// 
    /// URI: GET /api/contentinteractionapi/stats/{contentId}
    /// 
    /// 範例:
    /// GET https://localhost:7190/api/contentinteractionapi/stats/1
    /// </summary>
    [HttpGet("stats/{contentId}")]
    public async Task<IActionResult> GetInteractionStats(int contentId)
    {
      if (contentId <= 0) return BadRequest(new { message = "ContentId 必須大於 0" });

      var stats = await _contentInteractionService.GetInteractionStats(contentId);
      return Ok(stats);
    }
  }
}
