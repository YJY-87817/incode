
namespace incode.MVC.Services.PointShop
{
    public interface IOrderService
    {
        // ===== 查詢 =====
        Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrderAsync(int userId);
        Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetailAsync(int orderId);
        Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetailAsync(int orderId);
        Task<ResultDTO<IEnumerable<ShipAddressDTO>>> GetHasShippedDetailAsync(int userId);

        /// <summary>取得會員自己的訂單（唯讀）；不存在或不是自己的一律回傳 null，呼叫端回 404 以免透露訂單是否存在。</summary>
        Task<Orders?> GetOwnOrderAsync(int orderId, int userId);

        /// <summary>依寄送資料編號取得其所屬、且屬於該會員的訂單；找不到或不是自己的回傳 null。</summary>
        Task<Orders?> GetOwnOrderByShippedDetailAsync(int shippedDetailId, int userId);

        // ===== 異動 (皆在交易內完成：庫存原子扣減、點數同步、失敗整筆 rollback) =====
        // 擁有者 / 訂單狀態等權限檢查由呼叫端 (前台 API、後台) 負責

        /// <summary>
        /// 建立訂單。requireRecipient = true 時，含實體商品必須提供完整寄送資料 (前台)；
        /// 後台建單可不帶寄送資料，另由出貨管理建立
        /// </summary>
        Task<ResultDTO<int>> CreateOrderAsync(int userId, IEnumerable<OrderItemRequest>? items,
            NewShippedRequest? recipient, string? remark, bool requireRecipient,
            string status = Constants.OrderStatus.Pending);

        /// <summary>修改訂單明細，依新舊總點數差額補扣或退還點數</summary>
        Task<ResultDTO<int>> UpdateOrderItemsAsync(int orderId, IEnumerable<OrderItemRequest>? items, string? remark);

        /// <summary>取消訂單：退還庫存與點數</summary>
        Task<ResultDTO<int>> CancelOrderAsync(int orderId);

        /// <summary>修改寄送資料</summary>
        Task<ResultDTO<int>> EditShippedAsync(int shippedDetailId, NewShippedRequest shipped);
    }
}
