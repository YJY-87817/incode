using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Extensions
{
    /// <summary>
    /// 會員 Cookie 驗證：帳號被停用（IsActive = false）或已不存在時，已登入的人下一個請求就登出。
    /// 否則 Cookie 效期 7 天且會自動延長，停用帳號等於沒有立即生效。
    /// 結果快取 <see cref="CacheSeconds"/> 秒避免每個請求都查資料庫；停用 / 啟用時由 <see cref="Evict"/> 清除。
    /// </summary>
    public static class MemberCookieValidator
    {
        public const int CacheSeconds = 60;

        private static string CacheKey(int userId) => $"user-active-{userId}";

        public static void Evict(IMemoryCache cache, int userId) => cache.Remove(CacheKey(userId));

        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var userId = context.Principal?.GetId() ?? 0;
            if (userId <= 0) return;

            var services = context.HttpContext.RequestServices;
            var cache = services.GetRequiredService<IMemoryCache>();

            if (!cache.TryGetValue(CacheKey(userId), out bool active))
            {
                var db = services.GetRequiredService<incodedatabaseContext>();
                var flag = await db.User
                    .AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.IsActive })
                    .FirstOrDefaultAsync();

                // 查無會員（已刪除）或明確被停用（false）都視為無效；null 視為啟用（與登入檢查一致）
                active = flag != null && flag.IsActive != false;
                cache.Set(CacheKey(userId), active, TimeSpan.FromSeconds(CacheSeconds));
            }

            if (!active)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(context.Scheme.Name);
            }
        }
    }
}
