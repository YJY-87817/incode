namespace incode.MVC.Services.PointShop
{
    /// <summary>
    /// 後台寄送紀錄。不合法的輸入丟 BusinessException（訊息可以直接顯示）。
    /// 訂單狀態相關規則（已取消不能新增、已出貨 / 已完成不能刪除）都以單一條件語句 / 鎖判斷，不會被同時進行的狀態修改繞過。
    /// </summary>
    public interface IAdminShippedService
    {
        /// <summary>含所屬訂單（訂單頁「寄送管理」與寄送管理頁使用）</summary>
        Task<List<ShippedDetail>> GetShippedListAsync();

        Task<ShippedDetail?> GetShippedAsync(int shippedDetailId);

        /// <summary>已取消的訂單不能新增寄送資料</summary>
        Task<int> CreateShippedAsync(ShippedDetail form);

        /// <summary>只改收件資料、日期、備註與狀態；所屬訂單不可變更。找不到回傳 false</summary>
        Task<bool> UpdateShippedAsync(int shippedDetailId, ShippedDetail form);

        /// <summary>已出貨 / 已完成訂單的寄送紀錄不能刪除（出貨證明）。找不到回傳 false</summary>
        Task<bool> DeleteShippedAsync(int shippedDetailId);
    }
}
