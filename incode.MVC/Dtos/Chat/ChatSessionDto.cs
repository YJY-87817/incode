using System;

namespace incode.MVC.Dtos.Chat
{
    public class ChatSessionDto
    {
        public long ChatSessionId { get; set; } // 建議加上 ID
        public int UserId { get; set; }
        public string Title { get; set; }       // 對話標題
        public bool Ishumanmode { get; set; }   // ★ 重要：分辨目前是真人還是 AI
        public DateTime CreatedAt { get; set; }
    }
}