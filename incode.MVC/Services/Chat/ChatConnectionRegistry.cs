using System.Collections.Concurrent;

namespace incode.MVC.Services.Chat
{
    /// <summary>
    /// SignalR 連線屬於哪位會員（第三次全專案檢查 56）。聊天 API 會把前端傳來的 ConnectionId 加入對話群組，
    /// 原本不檢查這個連線是誰的：知道別人的 ConnectionId 就能把對方的連線加進自己的對話群組。
    /// ChatHub 連線 / 斷線時登記 / 移除，API 只把「登入者自己的連線」加入群組。單一伺服器的記憶體登記（與 SignalR 預設相同）
    /// </summary>
    public class ChatConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, int> _owners = new();

        public void Register(string connectionId, int userId)
        {
            if (!string.IsNullOrEmpty(connectionId) && userId > 0)
                _owners[connectionId] = userId;
        }

        public void Unregister(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
                _owners.TryRemove(connectionId, out _);
        }

        public bool IsOwnedBy(string? connectionId, int userId)
            => !string.IsNullOrEmpty(connectionId) && _owners.TryGetValue(connectionId, out var owner) && owner == userId;
    }
}
