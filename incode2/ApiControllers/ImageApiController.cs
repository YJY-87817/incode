using incode2.Services;
using Microsoft.AspNetCore.Mvc;

namespace incode2.ApiControllers
{
  [ApiController]
  [Route("/api/images")]
  public class MediaController : ControllerBase
  {
    private readonly IImageService _imageService;

    public MediaController(IImageService imageService)
    {
      _imageService = imageService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
    {
      try
      {
        if (files == null || files.Count == 0)
        {
          return BadRequest(new { message = "No files uploaded" });
        }

        var urls = await _imageService.UploadImagesAsync(files);
        return Ok(new { urls });
      }
      catch (Exception ex)
      {
        return BadRequest(new { message = ex.Message });
      }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromQuery] string path)
    {
      try
      {
        await _imageService.DeleteImageAsync(path);
        return Ok(new { message = "刪除成功" });
      }
      catch (Exception ex)
      {
        return BadRequest(new { message = ex.Message });
      }
    }
  }
}
