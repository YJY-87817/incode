namespace incode.MVC.Services.PointShop
{
    /// <summary>
    /// 後台點數商城（訂單、寄送紀錄、商品）。訂單的建立 / 修改 / 取消走 IOrderService（庫存、點數、交易都在那裡）；
    /// 這裡負責後台的查詢、輸入檢查與商品 / 寄送資料的維護。
    /// 不合法的輸入丟 BusinessException（訊息可以直接顯示）。
    /// </summary>
    public interface IAdminShopService
    {
        // ---------- 訂單 ----------
        Task<List<Orders>> GetOrdersAsync();

        Task<OrderViewModel?> GetOrderForEditAsync(int orderId);

        /// <summary>訂單表單的商品下拉選單（依名稱排序）</summary>
        Task<List<Product>> GetProductOptionsAsync();

        /// <summary>後台代會員建立訂單（總點數、單價、是否需寄送都由後端計算）</summary>
        Task<ResultDTO<int>> CreateOrderAsync(OrderViewModel form);

        /// <summary>修改訂單（明細、狀態、備註）；改為「已取消」會退還庫存與點數，已取消的訂單不能恢復</summary>
        Task<ResultDTO<int>> UpdateOrderAsync(int orderId, OrderViewModel form);

        // ---------- 寄送紀錄 ----------
        Task<List<ShippedDetail>> GetShippedListAsync();

        /// <summary>訂單頁 DataTables 使用的寄送資料</summary>
        Task<List<AdminShippedRow>> GetShippedRowsAsync();

        Task<ShippedDetail?> GetShippedAsync(int shippedDetailId);

        /// <summary>已取消的訂單不能新增寄送資料</summary>
        Task<int> CreateShippedAsync(ShippedDetail form);

        /// <summary>只改收件資料、日期、備註與狀態；所屬訂單不可變更。找不到回傳 false</summary>
        Task<bool> UpdateShippedAsync(int shippedDetailId, ShippedDetail form);

        /// <summary>已出貨 / 已完成訂單的寄送紀錄不能刪除（出貨證明）</summary>
        Task DeleteShippedAsync(int shippedDetailId);

        // ---------- 商品 ----------
        Task<List<Product>> GetProductsAsync();

        Task<Product?> GetProductAsync(int productId);

        Task<int> CreateProductAsync(Product form, IFormFile? image);

        /// <summary>
        /// 修改商品（不含庫存）；stockDelta 不為 0 時以原子更新增減庫存（同一交易）。
        /// 原本表單上的庫存數字直接覆蓋資料庫，會蓋掉編輯期間會員下單扣掉的庫存。找不到回傳 false
        /// </summary>
        Task<bool> UpdateProductAsync(int productId, Product form, IFormFile? image, int stockDelta);
    }

    /// <summary>後台訂單頁的寄送資料（JSON 欄位名稱給 DataTables 使用）</summary>
    public class AdminShippedRow
    {
        public int ShippedDetailId { get; set; }
        public int OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientAddress { get; set; }
        public string? RecipientPhone { get; set; }
        public string ShippedDate { get; set; } = "";
        public string ShippedDateFormatted { get; set; } = "";
        public string ArrivalDate { get; set; } = "";
        public string ArrivalDateFormatted { get; set; } = "";
        public bool IsFailed { get; set; }
        public string? Remark { get; set; }
        public bool IsCompleted { get; set; }
    }
}
