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

        public async Task<ResultDTO<IEnumerable<ProductDTO>>> GetProduct()
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
                    Type = e.Type,
                    IsPhysical = e.IsPhysical,
                    Point = e.Point,
                    Stock = e.Stock,
                })
                .ToListAsync();

            return ResultDTO<IEnumerable<ProductDTO>>.Success(products);
        }

    }
}
