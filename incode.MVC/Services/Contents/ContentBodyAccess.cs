using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Services.Contents
{
    /// <summary>
    /// 論壇內文檔（/contents/*.html）能不能公開提供：只有「公開中的文章（is_active = 1）目前的內文」可以。
    /// 作者刪除、後台下架的文章，以及編輯後留在 content_history 的舊版本都不提供（第三次全專案檢查 49；
    /// 原本檔案一直留在網站上，直接開網址就能讀到）。後台管理員不受限制（見 ContentBodyAccessExtensions）。
    ///
    /// content.body 為 nvarchar(max)、沒有索引，所以「可以提供」的結果快取 <see cref="CacheSeconds"/> 秒；
    /// 刪除 / 編輯 / 下架時以 <see cref="Evict"/> 清除，下一個請求就看到新狀態。「不能提供」不快取（只會發生在已移除的內容，次數很少）
    /// </summary>
    public static class ContentBodyAccess
    {
        public const int CacheSeconds = 60;

        private static string CacheKey(string bodyPath) => "content-body-public:" + bodyPath.ToLowerInvariant();

        /// <summary>網址是否為論壇內文檔（/contents/檔名，不含子資料夾）</summary>
        public static bool IsBodyPath(PathString path)
        {
            if (!path.StartsWithSegments("/" + UploadFolders.ContentBodies, StringComparison.OrdinalIgnoreCase, out var rest))
                return false;
            var name = rest.Value?.TrimStart('/') ?? "";
            return name.Length > 0 && !name.Contains('/');
        }

        public static async Task<bool> IsPublicAsync(incodedatabaseContext context, IMemoryCache cache, string bodyPath)
        {
            if (cache.TryGetValue(CacheKey(bodyPath), out _))
                return true;

            bool isPublic = await context.Content
                .AsNoTracking()
                .AnyAsync(c => c.Body == bodyPath && c.IsActive == ContentStatus.Active);

            if (isPublic)
                cache.Set(CacheKey(bodyPath), true, TimeSpan.FromSeconds(CacheSeconds));
            return isPublic;
        }

        /// <summary>文章被刪除 / 下架，或編輯後舊內文不再使用時呼叫（cache 為 null 時略過，例如單元測試）</summary>
        public static void Evict(IMemoryCache? cache, string? bodyPath)
        {
            if (cache != null && !string.IsNullOrEmpty(bodyPath))
                cache.Remove(CacheKey(bodyPath));
        }
    }
}
