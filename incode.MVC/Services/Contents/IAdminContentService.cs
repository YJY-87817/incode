namespace incode.MVC.Services.Contents
{
    /// <summary>後台論壇內容查詢（文章列表 / 詳情、修改前後對照、修改紀錄；全部唯讀）。</summary>
    public interface IAdminContentService
    {
        Task<List<ContentViewModel>> GetContentsAsync();

        Task<ContentDetailViewModel?> GetContentDetailAsync(int contentId);

        /// <summary>每篇內容與它最近一次修改前的標題 / 內文</summary>
        Task<List<ContentEditViewModel>> GetLatestEditsAsync();

        Task<List<ContentHistory>> GetHistoriesAsync();

        Task<ContentHistory?> GetHistoryAsync(int contentHistoryId);
    }
}
