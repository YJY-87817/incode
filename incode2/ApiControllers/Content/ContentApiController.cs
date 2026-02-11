using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.Contents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;


namespace incode2.ApiControllers.Content
{
  [ApiController]
  [Route("api/[controller]")]
  public class ContentApiController : ControllerBase
  {
    private readonly IContentService _contentService;
    private readonly IContentEntrustMissionService _contentEntrustMissionService;

    public ContentApiController(IContentService contentService, IContentEntrustMissionService contentEntrustMissionService)
    {
      _contentService = contentService;
      _contentEntrustMissionService = contentEntrustMissionService;
    }

    //所有content
    //https://localhost:7190/api/ContentApi
    [HttpGet]
    public async Task<IActionResult> GetContents([FromQuery] int? categoryId, [FromQuery] string? keyword, [FromQuery] string? types, [FromQuery] string? sortBy, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      var result = await _contentService.GetContents(categoryId, page, pageSize, userId > 0 ? userId : null, keyword, types, sortBy);
      return Ok(result);
    }

    // 取得單個content詳細資料
    // GET https://localhost:7190/api/ContentApi/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(int id)
    {
      var content = await _contentService.GetContent(id);
      if (content == null) return NotFound(new { message = "找不到此內容" });
      return Ok(content);
    }

    // 創立新的貼文
    // POST https://localhost:7190/api/ContentApi
    // Body: CreateContentDto
    [HttpPost]
    //[Authorize]
    public async Task<IActionResult> CreateContent([FromBody] CreateContentDto dto)
    {
      //這行可以取得當下登入的user所有資料
      var userId = User.GetId();
      dto.UserId = userId;

      if (!ModelState.IsValid) return BadRequest(ModelState);
      var content = await _contentService.CreateContent(dto);
      return CreatedAtAction(nameof(GetContent), new { id = content.ContentId }, content);
    }

    // 創立新的貼文並視情況新增委託任務
    // POST https://localhost:7190/api/ContentApi/with-entrust-mission
    // Body: CreateContentWithEntrustMissionDto
    [HttpPost("with-entrust-mission")]
    public async Task<IActionResult> CreateContentWithEntrustMission([FromBody] CreateContentWithEntrustMissionDto dto)
    {
      var userId = User.GetId();
      if (userId <= 0)
      {
        return Unauthorized(new { message = "請先登入" });
      }

      if (!ModelState.IsValid) return BadRequest(ModelState);

      var result = await _contentEntrustMissionService.CreateContentWithEntrustMissionAsync(dto, userId);
      if (!result.Ok)
      {
        return StatusCode(result.Code, result);
      }

      return CreatedAtAction(nameof(GetContent), new { id = result.Data?.Content.ContentId }, result);
    }

    // 更新content內容
    // PUT https://localhost:7190/api/ContentApi/{id}
    // Body: UpdateContentDto
    // 需要登入，且只能更新自己的content
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContent(int id, [FromBody] UpdateContentDto dto)
    {
      //這行可以取得當下登入的user所有資料
      var currentUserId = User.GetId();
      //int.Parse(User.Claims.FirstOrDefault().Value);

      var updated = await _contentService.UpdateContent(id, dto, currentUserId);
      if (updated == null) return NotFound(new { message = "找不到此內容" });
      return Ok(updated);
    }

    // 軟刪除content
    // DELETE https://localhost:7190/api/ContentApi/{id}
    // 需要登入，且只能刪除自己的content
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
      var currentUserId = User.GetId();
      if (currentUserId <= 0)
      {
        return Unauthorized(new { message = "請先登入" });
      }

      var success = await _contentService.DeleteContent(id, currentUserId);
      if (!success) return NotFound(new { message = "找不到此內容或無權限刪除" });
      return Ok(new { message = "刪除成功" });
    }

    // 取得指定content的所有評論
    // GET https://localhost:7190/api/ContentApi/{id}/comments
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetCommentsByContentId(int id)
    {
      var comments = await _contentService.GetCommentsByContentId(id);
      return Ok(new
      {
        contentId = id,
        comments = comments,
        count = comments.Count
      });
    }

    // 取得指定content的子回覆 (分頁)
    // GET https://localhost:7190/api/ContentApi/{id}/children?types=2,3&page=1&pageSize=20
    [HttpGet("{id}/children")]
    public async Task<IActionResult> GetChildrenByContentId(int id, [FromQuery] string? types, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
      var result = await _contentService.GetChildrenByContentId(id, types, page, pageSize);
      return Ok(result);
    }

    // 取得當前登入使用者的所有貼文 (type=1)
    // GET https://localhost:7190/api/ContentApi/me/posts?page=1&pageSize=10
    // 需要登入
    [HttpGet("me/posts")]
    public async Task<IActionResult> GetMyPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var result = await _contentService.GetUserContentsByTypes(userId, new[] { 1 }, page, pageSize);
      
      // 轉換為包含舊欄位的格式以保持向下相容
      var resultObj = result as dynamic;
      return Ok(new
      {
        userId,
        type = 1,
        items = resultObj.items,
        count = resultObj.totalCount,
        totalCount = resultObj.totalCount,
        page = resultObj.page,
        pageSize = resultObj.pageSize,
        totalPages = resultObj.totalPages
      });
    }

    // 取得當前登入使用者的所有回覆 (type=2)
    // GET https://localhost:7190/api/ContentApi/me/replies?page=1&pageSize=10
    // 需要登入
    [HttpGet("me/replies")]
    public async Task<IActionResult> GetMyReplies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var result = await _contentService.GetUserContentsByTypes(userId, new[] { 2 }, page, pageSize);
      
      var resultObj = result as dynamic;
      return Ok(new
      {
        userId,
        type = 2,
        items = resultObj.items,
        count = resultObj.totalCount,
        totalCount = resultObj.totalCount,
        page = resultObj.page,
        pageSize = resultObj.pageSize,
        totalPages = resultObj.totalPages
      });
    }

    // 取得當前登入使用者的所有評論 (type=3,4)
    // GET https://localhost:7190/api/ContentApi/me/comments?page=1&pageSize=10
    // 需要登入
    [HttpGet("me/comments")]
    public async Task<IActionResult> GetMyComments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var result = await _contentService.GetUserContentsByTypes(userId, new[] { 3, 4 }, page, pageSize);
      
      var resultObj = result as dynamic;
      return Ok(new
      {
        userId,
        type = new[] { 3, 4 },
        items = resultObj.items,
        count = resultObj.totalCount,
        totalCount = resultObj.totalCount,
        page = resultObj.page,
        pageSize = resultObj.pageSize,
        totalPages = resultObj.totalPages
      });
    }

    // 取得當前登入使用者按讚的所有content
    // GET https://localhost:7190/api/ContentApi/me/liked-contents?page=1&pageSize=10
    // 需要登入
    [HttpGet("me/liked-contents")]
    public async Task<IActionResult> GetMyLikedContents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var result = await _contentService.GetUserInteractedContents(userId, 1, new[] { "like" }, page, pageSize);
      
      var resultObj = result as dynamic;
      return Ok(new
      {
        userId,
        interactionCategoryId = 1,
        items = resultObj.items,
        count = resultObj.totalCount,
        totalCount = resultObj.totalCount,
        page = resultObj.page,
        pageSize = resultObj.pageSize,
        totalPages = resultObj.totalPages
      });
    }

    // 取得當前登入使用者收藏的所有content
    // GET https://localhost:7190/api/ContentApi/me/bookmarked-contents?page=1&pageSize=10
    // 需要登入
    [HttpGet("me/bookmarked-contents")]
    public async Task<IActionResult> GetMyBookmarkedContents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var result = await _contentService.GetUserInteractedContents(userId, 2, new[] { "bookmark" }, page, pageSize);
      
      var resultObj = result as dynamic;
      return Ok(new
      {
        userId,
        interactionCategoryId = 2,
        items = resultObj.items,
        count = resultObj.totalCount,
        totalCount = resultObj.totalCount,
        page = resultObj.page,
        pageSize = resultObj.pageSize,
        totalPages = resultObj.totalPages
      });
    }
  }
}
