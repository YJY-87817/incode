using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Extensions
{
    /// <summary>
    /// 論壇內文檔（/contents/*.html）只在公開中的文章指向它時提供，其他一律 404（規則見 <see cref="ContentBodyAccess"/>）。
    /// 後台管理員（後台 Cookie）可以開啟已刪除 / 已下架的內文與舊版本（後台文章詳情、修改紀錄只顯示檔案路徑）。
    /// 放在 UseStaticFiles 之前：UploadRoot 與尚未搬檔、還在 wwwroot 的舊檔都涵蓋
    /// </summary>
    public static class ContentBodyAccessExtensions
    {
        public static IApplicationBuilder UseContentBodyAccess(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                if (!path.StartsWithSegments("/" + UploadFolders.ContentBodies, StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }

                var services = context.RequestServices;
                bool allowed = ContentBodyAccess.IsBodyPath(path)
                    && await ContentBodyAccess.IsPublicAsync(
                        services.GetRequiredService<incodedatabaseContext>(),
                        services.GetRequiredService<IMemoryCache>(),
                        path.Value!);

                if (!allowed && await IsAdminAsync(context))
                    allowed = true;

                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next();
            });
        }

        // UseAuthentication 在靜態檔之後，這裡自己驗證後台 Cookie（同樣經過 MemberCookieValidator：停用 / 降級的管理員無效）
        private static async Task<bool> IsAdminAsync(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.Cookies["Incode.Admin"]))
                return false;

            var result = await context.AuthenticateAsync(AuthSchemes.Admin);
            return result.Succeeded && result.Principal?.IsInRole("Admin") == true;
        }
    }
}
