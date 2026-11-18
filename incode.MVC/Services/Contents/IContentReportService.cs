namespace incode.MVC.Services.Contents
{
    /// <summary>
    /// 後台檢舉處理：列出待處理的檢舉、駁回、下架（content.is_active = 3 官方移除）、恢復上架。
    /// 每次處理都寫一筆 content_report_review；文章最後一次處理之後的檢舉才算「待處理」。
    /// 動作失敗（找不到、狀態不符）丟 BusinessException。
    /// </summary>
    public interface IContentReportService
    {
        Task<ContentReportIndexViewModel> GetIndexAsync(int recentReviewCount = 50);

        Task DismissAsync(int contentId, int adminUserId, string? note);

        Task RemoveAsync(int contentId, int adminUserId, string? note);

        Task RestoreAsync(int contentId, int adminUserId, string? note);
    }
}
