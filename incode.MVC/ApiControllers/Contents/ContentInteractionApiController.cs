using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Contents
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

    // 取得特定內容的所有互動記錄
    // GET https://localhost:7190/api/contentinteractionapi/content/{contentId}
    // Path Parameters:
    //   - contentId (int): 內容ID
    // Response: { contentId, interactions: [], count }
    // 範例: GET https://localhost:7190/api/contentinteractionapi/content/1
    [HttpGet("content/{contentId}")]
    public async Task<IActionResult> GetContentInteractions(int contentId)
    {
      // 互動資料由 ContentInteractionService 統一查詢
      var interactions = await _contentInteractionService.GetContentInteractions(contentId);
      return Ok(new
      {
        ContentId = contentId,
        Interactions = interactions,
        interactions.Count
      });
    }

    // 取得當前user對指定內容的所有互動(需登入)
    // GET https://localhost:7190/api/contentinteractionapi/user/{contentId}
    // 需要登入，使用者ID自動從Claims取得
    // 範例: GET https://localhost:7190/api/contentinteractionapi/user/3
    [HttpGet("user/{contentId}")]
    [Authorize]
    public async Task<IActionResult> GetUserInteraction(int contentId)
    {
      if (contentId <= 0)
        return BadRequest(new { message = "ContentId 必須大於 0" });

      // 從Claims 中取得當前使用者 ID
      var userId = User.GetId();
      if (userId == 0)
        return Unauthorized(new { message = "無法取得使用者ID" });

      // 交由 ContentInteractionService 查使用者互動
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

    // 新增或切換互動（Toggle）
    // 同一個 content + user + category 按一次新增，再按一次移除
    // 不同互動類別彼此不互斥，可同時存在
    // POST https://localhost:7190/api/contentinteractionapi
    // Request Body:
    // {
    //   "interactionCategoryId": 1,  // 互動分類ID (1=讚, 2=收藏等)
    //   "contentId": 5               // 內容ID
    // }
    // Response: { message, isRemoved, data } 
    // - isRemoved=true 表示移除互動，isRemoved=false 表示新增互動
    // 需要登入，使用者ID自動從Claims取得
    // 範例: POST https://localhost:7190/api/contentinteractionapi
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

      // 設定 userId，確保使用者只能互動自己的操作
      dto.UserId = userId;

      // 寫入/移除互動交由 ContentInteractionService 處理（只接受會員可設定的分類）
      var result = await _contentInteractionService.AddOrUpdateInteraction(dto);
      if (!result.Ok)
        return StatusCode(result.Code, new { message = result.Message });

      // 回傳新增互動或是移除互動
      return Ok(new
      {
        message = result.Message,
        isRemoved = result.Data == null,
        data = result.Data
      });
    }

    // 原本的 DELETE api/contentinteractionapi/{contentId}/{userId}/{interactionCategoryId} 沒有登入檢查、
    // userId 由網址指定（任何人都能刪除別人的互動，包含最佳解答紀錄），且前端沒有使用，已移除；取消互動請用 POST 切換

    // 取得互動分類列表
    // GET https://localhost:7190/api/contentinteractionapi/category
    // Response: 返回所有可用的互動分類列表
    // 範例: GET https://localhost:7190/api/contentinteractionapi/category
    [HttpGet("category")]
    public async Task<IActionResult> GetInteractionCategory()
    {
      var categories = await _contentInteractionService.GetInteractionCategory();
      return Ok(categories);
    }

    // 統計指定內容的互動數量（按互動分類）
    // GET https://localhost:7190/api/contentinteractionapi/stats/{contentId}
    // 範例: GET https://localhost:7190/api/contentinteractionapi/stats/1
    // Response: 返回各互動分類的統計數據
    [HttpGet("stats/{contentId}")]
    public async Task<IActionResult> GetInteractionStats(int contentId)
    {
      if (contentId <= 0) return BadRequest(new { message = "ContentId 必須大於 0" });

      var stats = await _contentInteractionService.GetInteractionStats(contentId);
      return Ok(stats);
    }

    // 指定最佳解答
    // POST https://localhost:7190/api/contentinteractionapi/best-answer
    [HttpPost("best-answer")]
    [Authorize]
    public async Task<IActionResult> SelectBestAnswer([FromBody] SelectBestAnswerDto dto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var userId = User.GetId();
      if (userId <= 0)
        return Unauthorized(new { message = "無法取得使用者ID" });

      if (dto.EntrustMissionId <= 0 || dto.ContentId <= 0)
        return BadRequest(new { message = "EntrustMissionId 和 ContentId 必須大於 0" });

      // 最佳解答流程由 ContentInteractionService 處理（含點數與任務狀態）
      var result = await _contentInteractionService.SelectBestAnswerAsync(dto.EntrustMissionId, dto.ContentId, userId);

      if (!result.Ok)
      {
        return result.Code switch
        {
          400 => BadRequest(result),
          401 => Unauthorized(result),
          403 => StatusCode(403, result),
          404 => NotFound(result),
          409 => Conflict(result),
          _ => StatusCode(500, result)
        };
      }

      return Ok(result);
    }

    // 查詢委託任務的最佳解答
    // GET https://localhost:7190/api/contentinteractionapi/best-answer/{entrustMissionId}
    // Path Parameters:
    //   - entrustMissionId (int): 委託任務ID
    // Response: { entrustMissionId, bestAnswerContentId }
    // 範例: GET https://localhost:7190/api/contentinteractionapi/best-answer/2
    [HttpGet("best-answer/{entrustMissionId}")]
    public async Task<IActionResult> GetBestAnswer(int entrustMissionId)
    {
      if (entrustMissionId <= 0)
        return BadRequest(new { message = "EntrustMissionId 必須大於 0" });

      var bestAnswerContentId = await _contentInteractionService.GetBestAnswerContentId(entrustMissionId);

      return Ok(new
      {
        EntrustMissionId = entrustMissionId,
        BestAnswerContentId = bestAnswerContentId
      });
    }

    // 查詢委託任務的最佳解答 (QueryString 版本)
    // GET https://localhost:7190/api/contentinteractionapi/best-answer?entrustMissionId=2
    [HttpGet("best-answer")]
    public async Task<IActionResult> GetBestAnswerByQuery([FromQuery] int entrustMissionId)
    {
      if (entrustMissionId <= 0)
        return BadRequest(new { message = "EntrustMissionId 必須大於 0" });

      var bestAnswerContentId = await _contentInteractionService.GetBestAnswerContentId(entrustMissionId);

      return Ok(new
      {
        EntrustMissionId = entrustMissionId,
        BestAnswerContentId = bestAnswerContentId
      });
    }

    // 依條件查詢互動 (QueryString 版本)
    // GET https://localhost:7190/api/contentinteractionapi?interactionCategoryId=5
    // GET https://localhost:7190/api/contentinteractionapi?entrustMissionId=2
    [HttpGet]
    public async Task<IActionResult> GetInteractions(
      [FromQuery] int? contentId,
      [FromQuery] int? userId,
      [FromQuery] int? interactionCategoryId,
      [FromQuery] int? entrustMissionId)
    {
      if (contentId.HasValue && contentId <= 0)
        return BadRequest(new { message = "ContentId 必須大於 0" });

      if (userId.HasValue && userId <= 0)
        return BadRequest(new { message = "UserId 必須大於 0" });

      if (interactionCategoryId.HasValue && interactionCategoryId <= 0)
        return BadRequest(new { message = "InteractionCategoryId 必須大於 0" });

      if (entrustMissionId.HasValue && entrustMissionId <= 0)
        return BadRequest(new { message = "EntrustMissionId 必須大於 0" });

      var interactions = await _contentInteractionService.GetInteractions(
        contentId,
        userId,
        interactionCategoryId,
        entrustMissionId);

      return Ok(new
      {
        Interactions = interactions,
        interactions.Count
      });
    }
  }
}
