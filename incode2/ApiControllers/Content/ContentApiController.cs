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

    public ContentApiController(IContentService contentService)
    {
      _contentService = contentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetContents([FromQuery] int? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
      var userId = User.GetId();
      var result = await _contentService.GetContents(categoryId, page, pageSize, userId > 0 ? userId : null);
      return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(int id)
    {
      var content = await _contentService.GetContent(id);
      if (content == null) return NotFound(new { message = "找不到此內容" });
      return Ok(content);
    }

    //創立貼文
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
      var success = await _contentService.DeleteContent(id);
      if (!success) return NotFound(new { message = "找不到此內容" });
      return Ok(new { message = "刪除成功" });
    }

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

    [HttpGet("me/posts")]
    public async Task<IActionResult> GetMyPosts()
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var contents = await _contentService.GetUserContentsByTypes(userId, new[] { 1 });
      return Ok(new
      {
        userId,
        type = 1,
        items = contents,
        count = contents.Count
      });
    }

    [HttpGet("me/replies")]
    public async Task<IActionResult> GetMyReplies()
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var contents = await _contentService.GetUserContentsByTypes(userId, new[] { 2 });
      return Ok(new
      {
        userId,
        type = 2,
        items = contents,
        count = contents.Count
      });
    }

    [HttpGet("me/comments")]
    public async Task<IActionResult> GetMyComments()
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var contents = await _contentService.GetUserContentsByTypes(userId, new[] { 3, 4 });
      return Ok(new
      {
        userId,
        type = new[] { 3, 4 },
        items = contents,
        count = contents.Count
      });
    }

    [HttpGet("me/liked-contents")]
    public async Task<IActionResult> GetMyLikedContents()
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var contents = await _contentService.GetUserInteractedContents(userId, 1, new[] { "like" });
      return Ok(new
      {
        userId,
        interactionCategoryId = 1,
        items = contents,
        count = contents.Count
      });
    }

    [HttpGet("me/bookmarked-contents")]
    public async Task<IActionResult> GetMyBookmarkedContents()
    {
      var userId = User.GetId();
      if (userId <= 0) return Unauthorized(new { message = "無法取得使用者ID" });

      var contents = await _contentService.GetUserInteractedContents(userId, 2, new[] { "bookmark" });
      return Ok(new
      {
        userId,
        interactionCategoryId = 2,
        items = contents,
        count = contents.Count
      });
    }
  }
}
