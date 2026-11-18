namespace incode.MVC.Services.Common
{
    /// <summary>後台首頁的統計資料（會員數、文章數、儲值金額、待處理訂單、每月新會員、發文排行）。</summary>
    public interface IAdminDashboardService
    {
        Task<HomeViewModel> GetDashboardAsync();
    }
}
