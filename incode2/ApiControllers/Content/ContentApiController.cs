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
      var result = await _contentService.GetContents(categoryId, page, pageSize);
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
      var userId = User.Claims.FirstOrDefault().Value;

      if (!ModelState.IsValid) return BadRequest(ModelState);
      var content = await _contentService.CreateContent(dto);
      return CreatedAtAction(nameof(GetContent), new { id = content.ContentId }, content);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContent(int id, [FromBody] UpdateContentDto dto)
    {
      var updated = await _contentService.UpdateContent(id, dto);
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
  }
}
