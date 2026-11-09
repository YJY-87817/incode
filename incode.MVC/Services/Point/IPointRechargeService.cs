namespace incode.MVC.Services.Point
{
    /// <summary>儲值訂單（建立、付款失敗標記、會員查詢）。付款成功加點在 <see cref="IPointLogService.CompleteRechargeAsync"/>。</summary>
    public interface IPointRechargeService
    {
        /// <summary>金額換點數的比率：1 元 = 10 點。前端顯示的方案點數也依此計算。</summary>
        const int PointsPerCash = 10;

        /// <summary>建立待付款訂單，點數一律由後端依金額計算。</summary>
        Task<PointRecharge> CreateOrderAsync(int userId, int cashAmount, string? paymentMethod);

        /// <summary>綠界回報付款失敗 / 取消：只更新尚未成功的訂單（已成功的不會被後到的失敗通知覆蓋），回傳更新筆數。</summary>
        Task<int> MarkFailedAsync(string orderNo, string? paymentTradeNo);

        /// <summary>會員查詢自己的訂單；不存在或不是自己的一律回傳 null。</summary>
        Task<RechargeOrderStatusDto?> GetUserOrderAsync(int userId, string orderNo);
    }
}
