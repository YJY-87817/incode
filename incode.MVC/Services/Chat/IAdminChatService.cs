namespace incode.MVC.Services.Chat
{
    /// <summary>
    /// 後台客服：對話與訊息只能查看。訊息由會員、AI、客服的即時對話產生，後台不能新增 / 修改 / 刪除，
    /// 否則可以偽造或竄改對話紀錄。
    /// </summary>
    public interface IAdminChatService
    {
        /// <summary>客服頁：左側對話列表、指定（或最新）對話的訊息。找不到指定的對話時回傳空的對話內容。</summary>
        Task<ChatIndexViewModel> GetIndexAsync(int? sessionId);

        /// <summary>對話列表（含會員），最近建立的在前。</summary>
        Task<List<ChatSession>> GetSessionsAsync();

        Task<ChatSession?> GetSessionAsync(int sessionId);
    }
}
