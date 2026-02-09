using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.PointShop
{
    public class ProductService : IProductService
    {
        private readonly incodedatabaseContext _context;

        public ProductService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<ResultDTO<IEnumerable<ProductDTO>>> GetProductAsync()
        {
            try
            {
                // 增加 ToListAsync() 讓資料庫查詢非同步執行
                var products = await _context.Product
                .Where(e => e.OnSale == true)
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
                if (products == null || !products.Any())
                {
                    return ResultDTO<IEnumerable<ProductDTO>>.Success(products, "目前沒有商品", 204);
                }

                return ResultDTO<IEnumerable<ProductDTO>>.Success(products);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<ProductDTO>>.Failure($"取得商品失敗: {ex.Message}");
            }

        }

        public async Task<ResultDTO<IEnumerable<ProductDTO>>> SearchProductAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    // 增加 ToListAsync() 讓資料庫查詢非同步執行
                    var products = await _context.Product
                    .Where(e => e.OnSale == true)
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
                    if (products == null || !products.Any())
                    {
                        return ResultDTO<IEnumerable<ProductDTO>>.Success(products, "目前沒有商品", 204);
                    }

                    return ResultDTO<IEnumerable<ProductDTO>>.Success(products);
                }
                else
                {
                    var products = await _context.Product
                    .Where(e => e.OnSale == true && (e.Name.Contains(keyword) || e.Description.Contains(keyword)))
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
                    if (products == null || !products.Any())
                    {
                        return ResultDTO<IEnumerable<ProductDTO>>.Success(products, "沒有找到相關商品", 204);
                    }
                    return ResultDTO<IEnumerable<ProductDTO>>.Success(products);
                }
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<ProductDTO>>.Failure($"搜尋商品失敗: {ex.Message}");
            }
        }
    }
}
