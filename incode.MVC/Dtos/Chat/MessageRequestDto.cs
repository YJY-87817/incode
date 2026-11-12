using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Chat
{
    public class MessageRequestDto
    {
        public int SessionId { get; set; }

        [Required(ErrorMessage = "訊息內容不能為空")]
        [StringLength(ChatLimits.MaxMessageLength, ErrorMessage = "訊息最多 {1} 字")]
        public string UserContent { get; set; }

        [Required(ErrorMessage = "SignalR 連線 ID 遺失")]
        [StringLength(ChatLimits.MaxConnectionIdLength)]
        public string ConnectionId { get; set; }
    }
}