using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Chat
{
    public class MessageRequestDto
    {
        public int SessionId { get; set; }

        [Required(ErrorMessage = "訊息內容不能為空")]
        public string UserContent { get; set; }

        [Required(ErrorMessage = "SignalR 連線 ID 遺失")]
        public string ConnectionId { get; set; }
    }
}