using incode2.Dtos;
using incode2.Dtos.PointShop;

namespace incode2.Services.PointShop
{
    public interface IOrderService
    {
        Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrderAsync(int userId);
        Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetailAsync(int orderId);
        Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetailAsync(int orderDetailId);
        Task<ResultDTO<int>> CreateOrderAsync(OrderRequest order);
        Task<ResultDTO<int>> CreateShippedAsync(NewShippedRequest shipped);
        Task<ResultDTO<int>> EditShippedAsync(int shippedDetailId, NewShippedRequest shipped);
        Task<ResultDTO<int>> EditOrderAsync(int orderId, OrderRequest order);
        Task<ResultDTO<int>> CancelOrderAsync(int orderId);
    }
}
