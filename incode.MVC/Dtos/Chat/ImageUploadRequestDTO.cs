using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Chat
{
    public class ImageUploadRequest
    {
        public int SessionId { get; set; }

        [Required]
        public IFormFile Image { get; set; } 

        [Required]
        public string ConnectionId { get; set; } // SignalR ID

        public string UserContent { get; set; }  // 用來接收前端使用者輸入的文字 (例如幫我分析這張圖)
    }
}
    
