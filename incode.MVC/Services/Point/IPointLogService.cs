
namespace incode.MVC.Services.Point
{
    public enum RechargeResult
    {
        Completed,          // 本次完成加點
        AlreadyProcessed,   // 先前已加點（綠界重送）
        NotFound
    }

    public interface IPointLogService
    {
        
        // 規定必須有一個方法可以根據 UserId 取得點數紀錄
        Task<IEnumerable<PointLogDTO>> GetUserLogsAsync(int userId);

        // 給後端 MVC 使用的詳細 ViewModel 清單 (包含複雜描述邏輯)
        Task<List<PointLogsViewModel>> GetDetailedLogsAsync(int userId);

        // 規定必須有一個方法可以查詢目前總餘額
        Task<int> GetCurrentBalanceAsync(int userId);

        Task TipAsync(int fromUserId, int toUserId, int ContentId, int amount);

        // 在目前的交易內鎖定會員點數（sp_getapplock，交易結束才釋放；多位會員依 ID 排序避免死結）。
        // 讀取「之後會影響點數異動的資料」（餘額、任務領獎狀態、委託狀態…）之前先呼叫，同一會員的請求才會排隊。
        Task LockUsersAsync(params int[] userIds);

        // 加扣點：必須在交易內呼叫，會自動鎖定該會員；只加入 PointLog，不會 SaveChanges
        Task<bool> ChangePointsAsync(int userId,
            int changeAmount,
            string sourceType,
            int? sourceId,
            PointType type,
            string? displaySource = null,
            string? displaySubTitle = null);

        // 綠界付款成功：以條件更新把儲值單改為 success，只有實際更新到的那一次會加點（重複回呼不會重複加點）
        Task<RechargeResult> CompleteRechargeAsync(string orderNo, string? paymentTradeNo);
    }
}
