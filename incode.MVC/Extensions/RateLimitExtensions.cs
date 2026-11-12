using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace incode.MVC.Extensions
{
    /// <summary>端點上以 [EnableRateLimiting(RateLimitPolicies.Xxx)] 套用</summary>
    public static class RateLimitPolicies
    {
        /// <summary>登入 / 註冊：同一 IP 每分鐘 20 次（會員與後台登入共用；共用網路的使用者共用額度，不宜太低）</summary>
        public const string Auth = "auth";

        /// <summary>忘記密碼寄信 / 重設密碼：同一 IP 每 15 分鐘 5 次（避免大量寄信與猜重設碼）</summary>
        public const string PasswordReset = "password-reset";

        /// <summary>
        /// 會呼叫 Gemini 的聊天端點（送出訊息、上傳圖片、自動命名）：每位會員每分鐘 10 次（未登入時依 IP）。
        /// 每則訊息最多呼叫 2 次 Gemini，「生圖」另外呼叫付費 API，沒有限制會被刷費用
        /// </summary>
        public const string AiChat = "ai-chat";

        /// <summary>允許匿名的檔案上傳（註冊時的大頭貼）：每位會員 / 每個 IP 每 10 分鐘 10 次</summary>
        public const string Upload = "upload";
    }

    public static class RateLimitExtensions
    {
        /// <summary>
        /// 依來源 IP 限制登入相關端點的請求次數，超過回 429。
        /// 注意：部署在反向代理 / 負載平衡後面時，RemoteIpAddress 會是代理的 IP（所有人共用同一個額度），
        /// 需先設定 ForwardedHeaders（只信任自己的代理）讓 RemoteIpAddress 變成使用者的 IP。
        /// </summary>
        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.AddPolicy(RateLimitPolicies.Auth, context => FixedWindowByIp(context, 20, TimeSpan.FromMinutes(1)));
                options.AddPolicy(RateLimitPolicies.PasswordReset, context => FixedWindowByIp(context, 5, TimeSpan.FromMinutes(15)));
                options.AddPolicy(RateLimitPolicies.AiChat, context => FixedWindowByUser(context, 10, TimeSpan.FromMinutes(1)));
                options.AddPolicy(RateLimitPolicies.Upload, context => FixedWindowByUser(context, 10, TimeSpan.FromMinutes(10)));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;
                    response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                    const string message = "嘗試次數過多，請稍後再試";
                    if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
                        await response.WriteAsJsonAsync(new { ok = false, code = 429, message }, cancellationToken);
                    else
                    {
                        response.ContentType = "text/plain; charset=utf-8";
                        await response.WriteAsync(message, cancellationToken);
                    }
                };
            });
        }

        // 已登入依會員 ID（共用網路的會員不會互相影響），未登入依 IP
        private static RateLimitPartition<string> FixedWindowByUser(HttpContext context, int permitLimit, TimeSpan window)
        {
            var userId = context.User.GetId();
            var key = userId > 0 ? $"user:{userId}" : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
            });
        }

        private static RateLimitPartition<string> FixedWindowByIp(HttpContext context, int permitLimit, TimeSpan window)
            => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                });
    }
}
