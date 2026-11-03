
namespace incode2.Services.Point
{
    public interface IPointLogService
    {
        
        // 規定必須有一個方法可以根據 UserId 取得點數紀錄
        Task<IEnumerable<PointLogDTO>> GetUserLogsAsync(int userId);

        // 給後端 MVC 使用的詳細 ViewModel 清單 (包含複雜描述邏輯)
        Task<List<PointLogsViewModel>> GetDetailedLogsAsync(int userId);

        // 規定必須有一個方法可以查詢目前總餘額
        Task<int> GetCurrentBalanceAsync(int userId);

        Task TipAsync(int fromUserId, int toUserId, int ContentId, int amount);

        Task<bool> ChangePointsAsync(int userId,
            int changeAmount,
            string sourceType,
            int? sourceId,
            PointType type,
            string? displaySource = null,
            string? displaySubTitle = null);

        Task AddPointsFromRechargeAsync(PointRecharge recharge);
    }
}
