namespace incode.MVC.Extensions
{
    /// <summary>
    /// 會員上傳的檔案（論壇內文 HTML、論壇 / 聊天圖片、大頭貼、商品圖片）一律加上：
    /// - Content-Security-Policy: sandbox：直接開啟檔案網址時，瀏覽器把它放在獨立的來源並禁止腳本，
    ///   不能以本站來源發出帶 Cookie 的請求（論壇內文 HTML 是會員送來的原始內容，前端顯示前才經 DOMPurify 過濾）
    /// - X-Content-Type-Options: nosniff：不讓瀏覽器把圖片猜成 HTML
    /// 前端以 fetch 讀內文、以 &lt;img&gt; 顯示圖片，不受這兩個標頭影響。
    /// 依網址判斷，所以 App:UploadRoot 與尚未搬檔、還留在 wwwroot 的舊檔都涵蓋。
    /// </summary>
    public static class UploadSecurityHeadersExtensions
    {
        private static readonly PathString[] UploadPaths =
        {
            "/" + UploadFolders.ContentBodies,
            "/" + UploadFolders.ForumImages,
            "/" + UploadFolders.ChatImages,
            "/" + UploadFolders.Avatars,
            "/" + UploadFolders.Products,
        };

        public static IApplicationBuilder UseUploadSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                if (UploadPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
                {
                    var headers = context.Response.Headers;
                    headers["Content-Security-Policy"] = "sandbox";
                    headers["X-Content-Type-Options"] = "nosniff";
                }

                await next();
            });
        }
    }
}
