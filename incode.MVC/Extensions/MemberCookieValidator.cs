using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Extensions
{
    /// <summary>
    /// 會員 / 後台 Cookie 驗證：帳號被停用（IsActive = false）或已不存在時，已登入的人下一個請求就登出；
    /// 後台 Cookie 另外要求帳號仍是管理員。否則 Cookie 效期內（會員 7 天且自動延長、後台 10 小時）停用等於沒有立即生效。
    /// 結果快取 <see cref="CacheSeconds"/> 秒避免每個請求都查資料庫；停用 / 啟用 / 後台編輯會員時由 <see cref="Evict"/> 清除。
    /// </summary>
    public static class MemberCookieValidator
    {
        public const int CacheSeconds = 60;

        private sealed record AccountState(bool Active, int? RoleId, string SecurityStamp);

        private static string CacheKey(int userId) => $"user-state-{userId}";

        public static void Evict(IMemoryCache cache, int userId) => cache.Remove(CacheKey(userId));

        /// <summary>會員 Cookie（MemberAuth）：帳號必須存在且啟用。</summary>
        public static Task ValidateAsync(CookieValidatePrincipalContext context)
            => ValidateCoreAsync(context, requireAdmin: false);

        /// <summary>後台 Cookie（AdminAuth）：帳號必須存在、啟用，且角色仍是管理員。</summary>
        public static Task ValidateAdminAsync(CookieValidatePrincipalContext context)
            => ValidateCoreAsync(context, requireAdmin: true);

        private static async Task ValidateCoreAsync(CookieValidatePrincipalContext context, bool requireAdmin)
        {
            var userId = context.Principal?.GetId() ?? 0;
            if (userId <= 0)
            {
                // 後台 Cookie 一定有會員 ID，沒有就是無效的 Cookie
                if (requireAdmin) await RejectAsync(context);
                return;
            }

            var services = context.HttpContext.RequestServices;
            var cache = services.GetRequiredService<IMemoryCache>();

            if (!cache.TryGetValue(CacheKey(userId), out AccountState? state) || state == null)
            {
                var db = services.GetRequiredService<incodedatabaseContext>();
                var row = await db.User
                    .AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.IsActive, u.RoleId, u.SecurityStamp })
                    .FirstOrDefaultAsync();

                // 查無會員（已刪除）或明確被停用（false）都視為無效；null 視為啟用（與登入檢查一致）
                state = new AccountState(row != null && row.IsActive != false, row?.RoleId, row?.SecurityStamp ?? "");
                cache.Set(CacheKey(userId), state, TimeSpan.FromSeconds(CacheSeconds));
            }

            // 登入戳記：重設 / 修改密碼後資料庫換新，舊 Cookie 的戳記對不上。
            // 戳記從未換過（null）的帳號，改版前沒有戳記的 Cookie 視為相符，不會因為上線而把所有人登出
            var cookieStamp = context.Principal?.FindFirst(AuthSchemes.SecurityStampClaim)?.Value ?? "";
            bool stampMatches = string.Equals(cookieStamp, state.SecurityStamp, StringComparison.Ordinal);

            bool valid = state.Active && stampMatches && (!requireAdmin || state.RoleId == (int)RoleType.Admin);
            if (!valid) await RejectAsync(context);
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}
