namespace incode.MVC.Services.Chat
{
    /// <summary>
    /// 客服聊天的資料存取（對話與訊息）。Controller / Hub 不直接使用 DbContext，
    /// 對話擁有者的判斷（IsOwnerAsync）也集中在這裡。
    /// </summary>
    public interface IChatService
    {
        /// <summary>該會員是否擁有這個對話。</summary>
        Task<bool> IsOwnerAsync(int sessionId, int userId);

        /// <summary>依 ID 取得對話（追蹤中的實體，不檢查擁有者）。</summary>
        Task<ChatSession?> FindSessionAsync(int sessionId);

        /// <summary>
        /// 取得指定對話；沒指定（0）或找不到時接續該會員最新的對話，都沒有就建立新的。
        /// 指定的對話不屬於該會員時回傳 Session = null。
        /// </summary>
        Task<(ChatSession? Session, bool IsNew)> GetOrInitSessionAsync(int userId, int requestedSessionId, string initialTitle = "我的對話");

        Task<ChatSession> CreateSessionAsync(int userId, string title);

        Task AddMessageAsync(int sessionId, string role, string body);

        /// <summary>切換真人 / AI 模式並更新時間。</summary>
        Task SetHumanModeAsync(ChatSession session, bool isHumanMode);

        /// <summary>目前正在請求真人客服的對話（含最後一則訊息預覽），客服後台使用。</summary>
        Task<List<SupportSessionDto>> GetSupportSessionsAsync();

        Task<List<ChatSessionDto>> GetUserSessionsAsync(int userId);

        Task<List<ChatMessageDto>> GetHistoryAsync(int sessionId);

        /// <summary>對話中的第一則使用者訊息（AI 命名用），沒有則回傳 null。</summary>
        Task<string?> GetFirstUserMessageAsync(int sessionId);

        Task SetTitleAsync(ChatSession session, string title);

        /// <summary>刪除對話與其所有訊息。</summary>
        Task DeleteSessionAsync(ChatSession session);
    }
}
