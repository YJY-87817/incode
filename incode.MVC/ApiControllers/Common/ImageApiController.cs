using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Common
{
  [ApiController]
  [Authorize] // 需登入才能上傳，避免匿名大量上傳檔案
  [Route("/api/images")]
  public class MediaController : ControllerBase
  {
    private const long MaxUploadRequestBytes = 100L * 1024 * 1024;

    private readonly IImageService _imageService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IImageService imageService, ILogger<MediaController> logger)
    {
      _imageService = imageService;
      _logger = logger;
    }

    // 上傳單個或多個圖片文件
    // POST https://localhost:7190/api/images/upload
    // Request: multipart/form-data with multiple files
    // Response: { urls: ["url1", "url2", ...] }
    // 單檔 20 MB（ImageFileValidator）、一次最多 20 個檔案（ImageService），整個請求最多 100 MB
    // （全站上限 30 MB，見 Program.cs；原本這裡與全站都是 500 MB）
    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
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
      catch (ArgumentException ex)
      {
        // 檔案格式 / 大小 / 數量不符：回傳檢查訊息
        return BadRequest(new { message = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "圖片上傳失敗");
        return StatusCode(500, new { message = "圖片上傳失敗，請稍後再試" });
      }
    }

    // 刪除指定路徑的圖片文件
    // DELETE https://localhost:7190/api/images/delete?path={imagePath}
    // Query Parameters:
    //   - path (string): 要刪除的圖片路徑
    // Response: { message: "刪除成功" }
    // 無法得知圖片屬於哪位會員，僅限後台管理員刪除
    [HttpDelete("delete")]
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public async Task<IActionResult> Delete([FromQuery] string path)
    {
      try
      {
        await _imageService.DeleteImageAsync(path);
        return Ok(new { message = "刪除成功" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { message = ex.Message });
      }
      catch (FileNotFoundException)
      {
        return NotFound(new { message = "檔案不存在" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "刪除圖片失敗 {Path}", path);
        return StatusCode(500, new { message = "刪除失敗，請稍後再試" });
      }
    }
  }
}
