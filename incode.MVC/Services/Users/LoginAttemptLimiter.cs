using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Services.Users
{
    /// <summary>
    /// 同一帳號登入失敗次數限制（會員與後台共用）：15 分鐘內失敗 5 次即暫停該帳號登入 15 分鐘，登入成功後歸零。
    /// 搭配 Program.cs 的 IP 速率限制（擋單一來源大量嘗試），這裡擋「換 IP 猜同一個帳號的密碼」。
    /// 計數存在記憶體：重啟會清除，多台伺服器時各自計算。
    /// </summary>
    public class LoginAttemptLimiter
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private readonly IMemoryCache _cache;
        private readonly object _sync = new();

        public LoginAttemptLimiter(IMemoryCache cache) => _cache = cache;

        private static string Key(string email) => $"login-fail:{email.Trim().ToLowerInvariant()}";

        public bool IsLockedOut(string email)
            => _cache.TryGetValue<Counter>(Key(email), out var c) && c!.Count >= MaxFailures;

        public void RecordFailure(string email)
        {
            lock (_sync)
            {
                var key = Key(email);
                if (!_cache.TryGetValue<Counter>(key, out var counter))
                {
                    // 視窗從第一次失敗起算，之後的失敗不會延長，最多鎖定到第一次失敗的 15 分鐘後
                    counter = new Counter();
                    _cache.Set(key, counter, Window);
                }
                counter!.Count++;
            }
        }

        public void Reset(string email) => _cache.Remove(Key(email));

        private sealed class Counter
        {
            public int Count;
        }
    }
}
