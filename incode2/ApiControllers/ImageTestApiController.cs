using incode2.Service;
using Microsoft.AspNetCore.Mvc;

namespace incode2.ApiControllers
{
    //api/Test
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IImageService _imageService;

        public MediaController(IImageService imageService)
        {
            _imageService = imageService;
        }


        [HttpPost("upload")]
        [Route("/api/image")]
        public async Task<IActionResult> Upload(IFormFile file) // [可自訂]：參數名稱 "file" 需與前端 form-data 的 key 一致
        {
            try
            {
                // 作用：呼叫 Service 執行存檔，並拿到圖片網址
                var imageUrl = await _imageService.UploadImageAsync(file);

                // 作用：回傳成功訊息與圖片網址給前端
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                // 作用：發生錯誤（如格式不對或磁碟滿了）時回傳錯誤訊息
                return BadRequest(new { message = ex.Message });
            }

        }
    }
}
