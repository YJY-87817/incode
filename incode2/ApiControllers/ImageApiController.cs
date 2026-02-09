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

    // 上傳單個或多個圖片文件
    // POST https://localhost:7190/api/images/upload
    // Request: multipart/form-data with multiple files
    // Response: { urls: ["url1", "url2", ...] }
    // 支援最大 500 MB 檔案上傳
    // tim 預設不能上傳限制為約28.6 MB，改設定成 500 MB
    // Program.cs設定後，在ImageApiController.cs內的上傳api要用[屬性類別]來解除預設限制
    [HttpPost("upload")]
    [RequestSizeLimit(524288000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
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

    // 刪除指定路徑的圖片文件
    // DELETE https://localhost:7190/api/images/delete?path={imagePath}
    // Query Parameters:
    //   - path (string): 要刪除的圖片路徑
    // Response: { message: "刪除成功" }
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
