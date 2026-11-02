using incode2.Constants;
using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Extensions;
using incode2.Models;
using incode2.Services.PointShop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace incode2.ApiControllers
{
    [EnableCors("VueApp")]
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly incodedatabaseContext _context;

        public OrderApiController(IOrderService orderService, incodedatabaseContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        private int userId => HttpContext.User.GetId();

        // 取得自己的訂單 (找不到時一律回 404，不透露訂單是否存在)
        private Task<Orders?> FindOwnOrderAsync(int orderId)
            => _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        // 會員端只有「待處理」的訂單可以修改 / 取消 / 改寄送資料
        private static ResultDTO<int>? CheckEditable(Orders order, string action)
            => order.Status == OrderStatus.Pending
                ? null
                : ResultDTO<int>.Failure($"訂單狀態為「{order.Status}」，不能{action}", 409);

        private static ResultDTO<T> NotFoundResult<T>() => ResultDTO<T>.Failure("找不到該訂單", 404);

        // 取得自己的訂單列表
        // GET: api/OrderApi/order
        [HttpGet("order")]
        public async Task<ActionResult<ResultDTO<IEnumerable<OrderDTO>>>> GetOrder()
            => this.ToActionResult(await _orderService.GetOrderAsync(userId));

        // 取得訂單明細
        // GET: api/OrderApi/orderdetail/{OrderId}
        [HttpGet("orderdetail/{orderId}")]
        public async Task<ActionResult<ResultDTO<IEnumerable<OrderDetailDTO>>>> GetOrderDetail(int orderId)
        {
            if (await FindOwnOrderAsync(orderId) == null)
                return this.ToActionResult(NotFoundResult<IEnumerable<OrderDetailDTO>>());

            return this.ToActionResult(await _orderService.GetOrderDetailAsync(orderId));
        }

        // 取得訂單寄送資料
        // GET: api/OrderApi/shippeddetail/{OrderId}
        [HttpGet("shippeddetail/{orderId}")]
        public async Task<ActionResult<ResultDTO<IEnumerable<ShippedDTO>>>> GetShippedDetail(int orderId)
        {
            if (await FindOwnOrderAsync(orderId) == null)
                return this.ToActionResult(NotFoundResult<IEnumerable<ShippedDTO>>());

            return this.ToActionResult(await _orderService.GetShippedDetailAsync(orderId));
        }

        // 取得輸入過的寄送資料
        // GET: api/OrderApi/shipaddress
        [HttpGet("shipaddress")]
        public async Task<ActionResult<ResultDTO<IEnumerable<ShipAddressDTO>>>> GetShipAddress()
            => this.ToActionResult(await _orderService.GetHasShippedDetailAsync(userId));

        // 新增訂單 (庫存 / 點數檢查與扣除皆在 Service 交易內完成)
        // POST: api/OrderApi/OrderPost
        [HttpPost("OrderPost")]
        public async Task<ActionResult<ResultDTO<int>>> CreateOrder([FromBody] OrderRequest order)
        {
            if (order == null)
                return this.ToActionResult(ResultDTO<int>.Failure("訂單資料有誤"));

            var result = await _orderService.CreateOrderAsync(
                userId, order.Items, order.Recipient, order.Remark, requireRecipient: true);
            return this.ToActionResult(result);
        }

        // 修改寄送資料
        // PUT: api/OrderApi/ShippedPut/{ShippedDetailId}
        [HttpPut("ShippedPut/{shippedDetailId}")]
        public async Task<ActionResult<ResultDTO<int>>> EditShipped(int shippedDetailId, [FromBody] NewShippedRequest shipped)
        {
            // 只能修改自己訂單的寄送資料
            var order = await _context.ShippedDetail
                .Where(s => s.ShippedDetailId == shippedDetailId && s.Order.UserId == userId)
                .Select(s => s.Order)
                .FirstOrDefaultAsync();

            if (order == null)
                return this.ToActionResult(ResultDTO<int>.Failure($"找不到編號為 {shippedDetailId} 的寄送資料", 404));

            var notEditable = CheckEditable(order, "修改寄送資料");
            if (notEditable != null)
                return this.ToActionResult(notEditable);

            return this.ToActionResult(await _orderService.EditShippedAsync(shippedDetailId, shipped));
        }

        // 修改訂單明細
        // PUT: api/OrderApi/OrderPut/{OrderId}
        [HttpPut("OrderPut/{orderId}")]
        public async Task<ActionResult<ResultDTO<int>>> EditOrder(int orderId, [FromBody] OrderRequest order)
        {
            if (order == null)
                return this.ToActionResult(ResultDTO<int>.Failure("訂單資料有誤"));

            var existing = await FindOwnOrderAsync(orderId);
            if (existing == null)
                return this.ToActionResult(NotFoundResult<int>());

            var notEditable = CheckEditable(existing, "修改");
            if (notEditable != null)
                return this.ToActionResult(notEditable);

            return this.ToActionResult(
                await _orderService.UpdateOrderItemsAsync(orderId, order.Items, order.Remark ?? ""));
        }

        // 取消訂單
        // PUT: api/OrderApi/CancelOrder/{OrderId}
        [HttpPut("CancelOrder/{orderId}")]
        public async Task<ActionResult<ResultDTO<int>>> CancelOrder(int orderId)
        {
            var existing = await FindOwnOrderAsync(orderId);
            if (existing == null)
                return this.ToActionResult(NotFoundResult<int>());

            if (existing.Status == OrderStatus.Canceled)
                return this.ToActionResult(ResultDTO<int>.Failure("訂單已取消", 409));

            // 已出貨 / 已完成 / 其他 狀態都不可由會員取消 (避免出貨後仍退點)
            var notEditable = CheckEditable(existing, "取消");
            if (notEditable != null)
                return this.ToActionResult(notEditable);

            return this.ToActionResult(await _orderService.CancelOrderAsync(orderId));
        }
    }
}
