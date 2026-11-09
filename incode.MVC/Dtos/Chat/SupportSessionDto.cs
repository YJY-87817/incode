namespace incode.MVC.Dtos.Chat
{
    /// <summary>客服後台的「等待真人客服」對話列表項目。</summary>
    public class SupportSessionDto
    {
        public int ChatSessionId { get; set; }
        public int UserId { get; set; }
        public string? Title { get; set; }
        public string LastUpdate { get; set; } = "";
        public string? LastMessage { get; set; }
    }
}
