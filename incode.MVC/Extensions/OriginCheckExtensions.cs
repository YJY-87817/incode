namespace incode.MVC.Extensions
{
    /// <summary>
    /// CSRF 防護：前台 API 與 SignalR 只接受來自允許來源（自己 + CORS 允許來源）的瀏覽器請求。
    ///
    /// 會員 Cookie 為 SameSite=None（前後端可能部署在不同網域），其他網站的頁面送出的表單 / fetch 也會帶上 Cookie。
    /// 瀏覽器送出跨站的 POST / PUT / DELETE 與 WebSocket 連線時一定會附上 Origin 標頭，且網頁無法偽造，
    /// 所以檢查 Origin 就能擋下冒用會員身分的請求。沒有 Origin / Referer 的請求不是瀏覽器跨站送出的（例如 curl、伺服器呼叫），放行。
    /// </summary>
    public static class OriginCheckExtensions
    {
        // 伺服器對伺服器的回呼（綠界付款通知），不經過瀏覽器，另以 CheckMacValue 驗證
        private static readonly string[] ExemptPaths =
        {
            "/api/PointRechargeApi/ecpay/return",
        };

        public static IApplicationBuilder UseApiOriginCheck(this IApplicationBuilder app, IEnumerable<string> allowedOrigins)
        {
            var allowed = new HashSet<string>(allowedOrigins.Select(o => o.TrimEnd('/')), StringComparer.OrdinalIgnoreCase);

            return app.Use(async (context, next) =>
            {
                var request = context.Request;
                var isHub = request.Path.StartsWithSegments("/chatHub");
                var isApi = request.Path.StartsWithSegments("/api");

                // API 只檢查會改變資料的方法；SignalR 連線（含 WebSocket 的 GET）全部檢查
                var needsCheck = isHub || (isApi && !HttpMethods.IsGet(request.Method)
                                                 && !HttpMethods.IsHead(request.Method)
                                                 && !HttpMethods.IsOptions(request.Method));

                if (needsCheck && !ExemptPaths.Any(p => request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
                {
                    var origin = GetRequestOrigin(request);
                    var self = $"{request.Scheme}://{request.Host}";

                    if (origin != null && !origin.Equals(self, StringComparison.OrdinalIgnoreCase) && !allowed.Contains(origin))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { ok = false, code = 403, message = "不允許的請求來源" });
                        return;
                    }
                }

                await next();
            });
        }

        // 優先使用 Origin；沒有時退回 Referer 的來源部分；都沒有回傳 null（非瀏覽器跨站請求）
        // Origin: null（sandbox iframe、data: 頁面等）視為不允許的來源
        private static string? GetRequestOrigin(HttpRequest request)
        {
            var origin = request.Headers.Origin.ToString();
            if (!string.IsNullOrEmpty(origin))
                return origin.TrimEnd('/');

            var referer = request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                return uri.GetLeftPart(UriPartial.Authority);

            return null;
        }
    }
}
