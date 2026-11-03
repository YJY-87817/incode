using System.Collections.Generic;

namespace incode2.ViewModels.Chat
{
    public class ChatIndexViewModel
    {
        // 所有對話列表
        public List<ChatSession> Sessions { get; set; }

        // 當前選中的對話訊息
        public List<ChatMessage> CurrentMessages { get; set; }

        // 當前選中的 Session ID
        public int? CurrentChatSessionId { get; set; }

        // ★ 新增：當前對話的目標會員 (用來抓取他的 ImageUrl)
        public User TargetUser { get; set; }
    }
}