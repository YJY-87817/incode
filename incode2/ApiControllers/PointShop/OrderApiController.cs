using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Services.PointShop;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using incode2.Extensions;
using Microsoft.AspNetCore.Authorization;


namespace incode2.ApiControllers
{
    [EnableCors("VueApp")]
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderApiController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int userId => HttpContext.User.GetId();

        // 取得UserId的訂單資料
        // GET: api/OrderApi/order
        [HttpGet("order")]
        public async Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrder() //登入才可找
        //[HttpGet("order/{UserId}")]
        //public async Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrder(int UserId)
        {
            return await _orderService.GetOrderAsync(userId);
        }


        // 取得OrderId的訂單資料
        // GET: api/OrderApi/orderdetail/{OrderId}
        [HttpGet("orderdetail/{OrderId}")]
        public async Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetail(int orderId)
        {
            return await _orderService.GetOrderDetailAsync(orderId);
        }


        // 取得ShippedDetailId的訂單資料
        // GET: api/OrderApi/shippeddetail/{OrderId}
        [HttpGet("shippeddetail/{OrderId}")]
        public async Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetail(int OrderId)
        {
            return await _orderService.GetShippedDetailAsync(OrderId);
        }


        // 新增訂單
        // POST: api/OrderApi/OrderPost
        [HttpPost("OrderPost")]
        public async Task<ResultDTO<int>> CreateOrder([FromBody] OrderRequest order)
        {
            order.UserId = userId;
            return await _orderService.CreateOrderAsync(order);
        }


        // 新增寄送資料
        // POST: api/OrderApi/ShippedPost
        [HttpPost("ShippedPost")]
        public async Task<ResultDTO<int>> CreateShipped([FromBody] NewShippedRequest shipped)
        {
            return await _orderService.CreateShippedAsync(shipped);
        }


        // 修改寄送資料
        // PUT: api/OrderApi/ShippedPut/{ShippedDetailId}
        [HttpPut("ShippedPut/{ShippedDetailId}")]
        public async Task<ResultDTO<int>> EditShipped(int ShippedDetailId, [FromBody] NewShippedRequest shipped)
        {
            return await _orderService.EditShippedAsync(ShippedDetailId, shipped);
        }


        // 修改訂單資料
        // PUT: api/OrderApi/OrderPut/{OrderId}
        [HttpPut("OrderPut/{OrderId}")]
        public async Task<ResultDTO<int>> EditOrder(int OrderId, [FromBody] OrderRequest order)
        {
            
            return await _orderService.EditOrderAsync(OrderId, order);
        }

        // 取消訂單
        // PUT: api/OrderApi/CancelOrder/{OrderId}
        [HttpPut("CancelOrder/{OrderId}")]
        public async Task<ResultDTO<int>> CancelOrder(int OrderId)
        {
            return await _orderService.CancelOrderAsync(OrderId);
        }

    }
}
