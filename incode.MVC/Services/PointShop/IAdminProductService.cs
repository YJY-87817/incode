namespace incode.MVC.Services.PointShop
{
    /// <summary>後台商品。不合法的輸入丟 BusinessException（訊息可以直接顯示）。</summary>
    public interface IAdminProductService
    {
        Task<List<Product>> GetProductsAsync();

        Task<Product?> GetProductAsync(int productId);

        Task<int> CreateProductAsync(Product form, IFormFile? image);

        /// <summary>
        /// 修改商品（不含庫存）；stockDelta 不為 0 時以原子更新增減庫存（同一交易）。
        /// 原本表單上的庫存數字直接覆蓋資料庫，會蓋掉編輯期間會員下單扣掉的庫存。找不到回傳 false
        /// </summary>
        Task<bool> UpdateProductAsync(int productId, Product form, IFormFile? image, int stockDelta);
    }
}
