using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;

namespace incode2.Services.PointShop
{
    public interface IOrderService
    {
        Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrderAsync(int userId);
        Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetailAsync(int orderId);
        Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetailAsync(int orderId);
        Task<ResultDTO<int>> CreateOrderAsync(OrderRequest order, int totalPoints, Dictionary<int, Product> productDict);
        Task<ResultDTO<int>> EditShippedAsync(ShippedDetail existingShipped, NewShippedRequest shipped);
        Task<ResultDTO<int>> EditOrderAsync(Orders oldOrder, OrderRequest order, Dictionary<int, Product> productDict);
        Task<ResultDTO<int>> CancelOrderAsync(Orders oldOrder, Dictionary<int, Product> productDict);
    }
}
