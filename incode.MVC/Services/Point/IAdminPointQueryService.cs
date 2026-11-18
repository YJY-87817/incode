namespace incode.MVC.Services.Point
{
    /// <summary>後台點數紀錄 / 儲值單的查詢（唯讀；調整與補單在 IPointLogService）。</summary>
    public interface IAdminPointQueryService
    {
        Task<List<PointLog>> GetLogsAsync();

        Task<PointLog?> GetLogAsync(int pointLogId);

        Task<List<PointRecharge>> GetRechargesAsync();

        Task<PointRecharge?> GetRechargeAsync(int pointRechargeId);

        /// <summary>下拉選單用的會員清單（依 ID 排序）</summary>
        Task<List<(int UserId, string Username, string Email)>> GetUserOptionsAsync();

        /// <summary>單一會員的點數明細（新到舊）與目前餘額</summary>
        Task<(List<PointLogsViewModel> Logs, int CurrentBalance)> GetUserLedgerAsync(int userId);
    }
}
