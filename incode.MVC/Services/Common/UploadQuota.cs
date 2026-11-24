using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Services.Common
{
    /// <summary>
    /// 每位會員每小時可以寫入的檔案量（第三次全專案檢查 50）：論壇圖片上傳、發文 / 編輯的內文檔、聊天圖片共用同一個額度，
    /// 超過時 API 回 429（<see cref="ExceededMessage"/>）。原本已登入的會員可以不停上傳 / 發文塞滿磁碟。
    ///
    /// 額度由 <c>App:UploadQuotaMegabytesPerHour</c> 設定（預設 <see cref="DefaultMegabytesPerHour"/> MB，0 = 不限制）。
    /// 時間窗從該小時第一次寫入起算（與 LoginAttemptLimiter 相同）；計數存在記憶體，重啟會清除，多台伺服器時各自計算。
    /// 匿名的大頭貼上傳沒有會員可以計算，另由 RateLimitPolicies.Upload 依 IP 限制
    /// </summary>
    public class UploadQuota
    {
        public const int DefaultMegabytesPerHour = 200;
        public static readonly TimeSpan Window = TimeSpan.FromHours(1);

        private readonly IMemoryCache _cache;
        private readonly object _sync = new();

        public UploadQuota(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            var megabytes = configuration.GetValue("App:UploadQuotaMegabytesPerHour", DefaultMegabytesPerHour);
            LimitBytes = megabytes <= 0 ? long.MaxValue : megabytes * 1024L * 1024L;
            ExceededMessage = megabytes <= 0
                ? "上傳量已達上限，請稍後再試"
                : $"上傳量已達上限（每小時 {megabytes} MB），請稍後再試";
        }

        /// <summary>每小時上限（位元組）</summary>
        public long LimitBytes { get; }

        /// <summary>超過額度時回給前端的訊息</summary>
        public string ExceededMessage { get; }

        private static string Key(int userId) => $"upload-quota:{userId}";

        /// <summary>
        /// 預扣這次要寫入的量：沒超過額度就記下並回傳 true；超過回傳 false（不記錄，該請求不應寫入任何檔案）。
        /// 在實際寫檔前呼叫；寫入因檢查失敗而沒有發生時不退回額度（避免反覆送出不合法的大檔案來試探）
        /// </summary>
        public bool TryConsume(int userId, long bytes)
        {
            if (bytes <= 0 || LimitBytes == long.MaxValue) return true;

            lock (_sync)
            {
                var key = Key(userId);
                if (!_cache.TryGetValue<Counter>(key, out var counter))
                {
                    counter = new Counter();
                    _cache.Set(key, counter, Window);
                }

                if (counter!.Bytes + bytes > LimitBytes)
                    return false;

                counter.Bytes += bytes;
                return true;
            }
        }

        /// <summary>文字內容（論壇內文檔以 UTF-8 存檔）佔用的位元組數</summary>
        public static long TextBytes(string? text) => string.IsNullOrEmpty(text) ? 0 : System.Text.Encoding.UTF8.GetByteCount(text);

        private sealed class Counter
        {
            public long Bytes;
        }
    }
}
