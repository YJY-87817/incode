namespace incode.MVC.Services.Common
{
    /// <summary>
    /// 分頁參數校正（第二次全專案檢查 38）：page 至少 1；pageSize 小於等於 0 用預設值，最多 <see cref="MaxPageSize"/>。
    /// SQL Server 的 OFFSET … FETCH 遇到負數或 0 會丟錯（500），極大的 pageSize 會一次撈出全部資料。
    /// 需要全部資料時，前端依回應的 totalPages 逐頁取得（例如 NewTopicModal 的主題清單）。
    /// </summary>
    public static class Paging
    {
        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultPageSize = 10)
            => (Math.Max(1, page), pageSize <= 0 ? defaultPageSize : Math.Min(pageSize, MaxPageSize));
    }
}
