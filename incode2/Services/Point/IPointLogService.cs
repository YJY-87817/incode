using incode2.Constants;
using incode2.DTO;
using incode2.Dtos;
using incode2.Models;
using incode2.ViewModels;

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

        //儲值 => 入帳（綠界 ReturnURL 用） 不需要回傳值 成功就是成功 錯誤直接 throw
        Task RechargeSuccessAsync(string orderNo);

        //兌換商品（扣點） 扣點一定要知道：誰 扣多少 為了什麼
        Task ConsumeAsync(int userId, int points, int OrderId);

        //打賞影評作者（雙向交易）//寫 2 筆 point_log 包在同一個 Transaction
        Task TipAsync(int fromUserId, int toUserId, int ContentId, int amount);

        Task<ResultDTO> AddPointsAsync(int userId, int rewardPoints, int missionId);

        Task<bool> ChangePointsAsync(int userId, int changeAmount, string sourceType, int sourceId, PointType type);

        //Task<bool> DeductPointsAsync(int userId, int changeAmount, string sourceType, int sourceId, PointType type);
        Task AddPointsFromRechargeAsync(PointRecharge recharge);
    }
}