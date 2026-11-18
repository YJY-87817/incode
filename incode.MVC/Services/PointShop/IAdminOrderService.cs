namespace incode.MVC.Services.PointShop
{
    /// <summary>
    /// 後台訂單。訂單的建立 / 修改 / 取消走 IOrderService（庫存、點數、交易都在那裡）；
    /// 這裡負責後台的查詢與表單規則（狀態、已取消不可恢復）。
    /// </summary>
    public interface IAdminOrderService
    {
        Task<List<Orders>> GetOrdersAsync();

        Task<OrderViewModel?> GetOrderForEditAsync(int orderId);

        /// <summary>訂單表單的商品下拉選單（依名稱排序）</summary>
        Task<List<Product>> GetProductOptionsAsync();

        /// <summary>後台代會員建立訂單（總點數、單價、是否需寄送都由後端計算）</summary>
        Task<ResultDTO<int>> CreateOrderAsync(OrderViewModel form);

        /// <summary>
        /// 修改訂單（明細、狀態、備註）；改為「已取消」會退還庫存與點數，已取消的訂單不能恢復。
        /// 只有「找不到該訂單」回傳 Code 404；明細中的商品不存在等錯誤回傳 400（讓表單能顯示訊息）
        /// </summary>
        Task<ResultDTO<int>> UpdateOrderAsync(int orderId, OrderViewModel form);
    }
}
