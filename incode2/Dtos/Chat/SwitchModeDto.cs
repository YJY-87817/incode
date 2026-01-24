namespace incode2.Dtos.Chat
{
    // 用於切換 真人/AI 模式
    public class SwitchModeDto
    {
        public int SessionId { get; set; }

        // true = 切換為真人客服
        // false = 切換回 AI
        public bool Ishumanmode { get; set; }

        // 用於 SignalR 通知前端更新狀態
        public string ConnectionId { get; set; }
    }
}