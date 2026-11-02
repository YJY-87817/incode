using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.PointShop
{
    public class ProductService : IProductService
    {
        private readonly incodedatabaseContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(incodedatabaseContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<ResultDTO<IEnumerable<ProductDTO>>> GetProductAsync()
            => QueryOnSaleProductsAsync(null);

        public Task<ResultDTO<IEnumerable<ProductDTO>>> SearchProductAsync(string keyword)
            => QueryOnSaleProductsAsync(keyword);

        // 查詢上架中商品，keyword 為空時回傳全部 (比對名稱與描述)
        private async Task<ResultDTO<IEnumerable<ProductDTO>>> QueryOnSaleProductsAsync(string? keyword)
        {
            try
            {
                var query = _context.Product.AsNoTracking().Where(e => e.OnSale);

                var trimmed = keyword?.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    query = query.Where(e => e.Name.Contains(trimmed) || e.Description.Contains(trimmed));

                var products = await query
                    .Select(e => new ProductDTO
                    {
                        ProductId = e.ProductId,
                        Name = e.Name,
                        Description = e.Description,
                        ImageName = e.ImageName,
                        Series = e.Series,
                        Type = e.Type,
                        IsPhysical = e.IsPhysical,
                        Point = e.Point,
                        Stock = e.Stock,
                    })
                    .ToListAsync();

                var message = products.Count > 0
                    ? "操作成功"
                    : string.IsNullOrEmpty(trimmed) ? "目前沒有商品" : "沒有找到相關商品";

                return ResultDTO<IEnumerable<ProductDTO>>.Success(products, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢商品失敗 (keyword: {Keyword})", keyword);
                return ResultDTO<IEnumerable<ProductDTO>>.Failure("取得商品失敗，請稍後再試", 500);
            }
        }
    }
}
