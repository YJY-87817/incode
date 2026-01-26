using incode2.Dtos;
using incode2.Dtos.PointShop;

namespace incode2.Services.PointShop
{
    public interface IProductService
    {
        Task<ResultDTO<IEnumerable<ProductDTO>>> GetProduct();
    }
}
