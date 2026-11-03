
namespace incode2.Services.PointShop
{
    public interface IProductService
    {
        Task<ResultDTO<IEnumerable<ProductDTO>>> GetProductAsync();
        Task<ResultDTO<IEnumerable<ProductDTO>>> SearchProductAsync(string keyword);
    }
}
