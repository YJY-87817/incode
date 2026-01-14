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
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                var imageUrl = await _imageService.UploadImageAsync(file);
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
