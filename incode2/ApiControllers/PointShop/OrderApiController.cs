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


        public OrderApiController(IOrderService orderService , incodedatabaseContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        private int userId => HttpContext.User.GetId();

        // 會員端只有「待處理」的訂單可以修改 / 取消 / 改寄送資料
        // (後台狀態有：待處理、已出貨、已完成、已取消、其他)
        private const string EditableStatus = "待處理";

        // 檢查訂單是否屬於目前登入會員
        private Task<bool> IsOwnOrderAsync(int orderId)
            => _context.Orders.AnyAsync(o => o.OrderId == orderId && o.UserId == userId);

        // 檢查訂單明細數量皆為正數
        private static bool HasInvalidQuantity(OrderRequest order)
            => order.Items.Any(i => i == null || i.Quantity <= 0);

        // 取得UserId的訂單資料
        // GET: api/OrderApi/order
        [HttpGet("order")]
        public async Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrder() //登入才可找
        {
            var result = await _orderService.GetOrderAsync(userId);
            return result;
        }


        // 取得OrderId的訂單資料
        // GET: api/OrderApi/orderdetail/{OrderId}
        [HttpGet("orderdetail/{OrderId}")]
        public async Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetail(int orderId)
        {
            if (!await IsOwnOrderAsync(orderId))
                return ResultDTO<IEnumerable<OrderDetailDTO>>.Failure("找不到該訂單", 404);

            var result = await _orderService.GetOrderDetailAsync(orderId);
            return result;
        }


        // 取得ShippedDetailId的訂單資料
        // GET: api/OrderApi/shippeddetail/{OrderId}
        [HttpGet("shippeddetail/{OrderId}")]
        public async Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetail(int OrderId)
        {
            if (!await IsOwnOrderAsync(OrderId))
                return ResultDTO<IEnumerable<ShippedDTO>>.Failure("找不到該訂單", 404);

            var result = await _orderService.GetShippedDetailAsync(OrderId);
            return result;
        }

        // 取得輸入過寄送資料
        // GET: api/OrderApi/shipaddress
        [HttpGet("shipaddress")]
        public async Task<ResultDTO<IEnumerable<ShipAddressDTO>>> GetShipAddress() //登入才可找
        {
            var result = await _orderService.GetHasShippedDetailAsync(userId);
            return result;
        }


        // 新增訂單
        // POST: api/OrderApi/OrderPost
        [HttpPost("OrderPost")]
        public async Task<ResultDTO<int>> CreateOrder([FromBody] OrderRequest order)
        {
            // 驗證訂單資料
            if (order == null || order.Items == null || !order.Items.Any())
                return ResultDTO<int>.Failure("訂單資料有誤");

            if (HasInvalidQuantity(order))
                return ResultDTO<int>.Failure("商品數量必須大於 0");

            order.UserId = userId;

            // 檢查 user 是否存在
            var user = await _context.User.FindAsync(order.UserId);
            if (user == null)
                return ResultDTO<int>.Failure($"找不到該會員{order.UserId}");

            // 取得所有商品
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var productDict = await _context.Product
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // 檢查商品是否都存在
            foreach (var item in order.Items)
            {
                if (!productDict.TryGetValue(item.ProductId, out var prod))
                    return ResultDTO<int>.Failure($"無此商品{item.ProductId}");

                if (prod.Stock < item.Quantity)
                    return ResultDTO<int>.Failure($"商品 {prod.Name} 庫存不足");
            }

            // 計算總點數
            int totalPoints = order.Items.Sum(item => item.Quantity * productDict[item.ProductId].Point);

            // 檢查會員點數是否足夠
            var usertotalpoint = await _context.PointLog.Where(p => p.UserId == userId).SumAsync(p => p.ChangeAmount);
            if (usertotalpoint < totalPoints)
                return ResultDTO<int>.Failure("會員點數不足");

            // 交易建立訂單與明細
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await _orderService.CreateOrderAsync(order, totalPoints, productDict);

            if (result.Ok)
            {
                await transaction.CommitAsync();
                return result;
            }
            else 
            {
                await transaction.RollbackAsync();
                return result;
            }
        }

        // 修改寄送資料
        // PUT: api/OrderApi/ShippedPut/{ShippedDetailId}
        [HttpPut("ShippedPut/{ShippedDetailId}")]
        public async Task<ResultDTO<int>> EditShipped(int ShippedDetailId, [FromBody] NewShippedRequest shipped)
        {
            // 驗證是否空
            if (shipped == null)
                return ResultDTO<int>.Failure("寄送資料錯誤");

            // 找出資料庫中現有的資料
            var existingShipped = await _context.ShippedDetail.FindAsync(ShippedDetailId);

            if (existingShipped == null)
                return ResultDTO<int>.Failure($"找不到編號為 {ShippedDetailId} 的寄送資料");

            // 只能修改自己訂單的寄送資料
            var shippedOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == existingShipped.OrderId && o.UserId == userId);
            if (shippedOrder == null)
                return ResultDTO<int>.Failure($"找不到編號為 {ShippedDetailId} 的寄送資料");

            // 檢查邏輯
            if (existingShipped.IsCompleted)
                return ResultDTO<int>.Failure("已完成的寄送資料不可修改");

            if (shippedOrder.Status != EditableStatus)
                return ResultDTO<int>.Failure($"訂單狀態為「{shippedOrder.Status}」，不能修改寄送資料");

            return await _orderService.EditShippedAsync(existingShipped, shipped);
        }


        // 修改訂單資料
        // PUT: api/OrderApi/OrderPut/{OrderId}
        [HttpPut("OrderPut/{OrderId}")]
        public async Task<ResultDTO<int>> EditOrder(int OrderId, [FromBody] OrderRequest order)
        {
            // 驗證訂單資料
            if (order == null || order.Items == null || !order.Items.Any())
                return ResultDTO<int>.Failure("訂單資料有誤");

            if (HasInvalidQuantity(order))
                return ResultDTO<int>.Failure("商品數量必須大於 0");

            // 同一商品不可重複出現 (避免庫存 / 點數計算錯誤)
            if (order.Items.Select(i => i.ProductId).Distinct().Count() != order.Items.Count)
                return ResultDTO<int>.Failure("訂單明細有重複的商品");

            // 取得修改前訂單資料 (只能取自己的訂單)
            var oldOrder = await _context.Orders.Include(o => o.Orderdetail)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId && o.UserId == userId);

            if (oldOrder == null)
                return ResultDTO<int>.Failure("找不到該訂單");

            if (oldOrder.Status != EditableStatus)
                return ResultDTO<int>.Failure($"訂單狀態為「{oldOrder.Status}」，不能修改");

            // 取得所有相關商品 (包含新舊商品)
            var productIds = order.Items.Select(i => i.ProductId)
                .Concat(oldOrder.Orderdetail.Select(d => d.ProductId))
                .Distinct()
                .ToList();

            var productDict = await _context.Product
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // 檢查商品是否都存在
            foreach (var item in order.Items)
            {
                if (!productDict.TryGetValue(item.ProductId, out var prod))
                    return ResultDTO<int>.Failure($"找不到商品 {item.ProductId}");
            }

            // 檢查庫存是否足夠
            foreach (var itemDto in order.Items)
            {
                var prod = productDict[itemDto.ProductId];
                var oldDetail = oldOrder.Orderdetail.FirstOrDefault(d => d.ProductId == itemDto.ProductId);

                int diff = itemDto.Quantity - (oldDetail?.Quantity ?? 0);
                if (prod.Stock < diff)
                    return ResultDTO<int>.Failure($"{prod.Name} 庫存不足");
            }

            // 開啟交易
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await _orderService.EditOrderAsync(oldOrder, order, productDict);

            if (result.Ok)
            {
                await transaction.CommitAsync();
                return result;
            }
            else
            {
                await transaction.RollbackAsync();
                return result;
            }
        }

        // 取消訂單
        // PUT: api/OrderApi/CancelOrder/{OrderId}
        [HttpPut("CancelOrder/{OrderId}")]
        public async Task<ResultDTO<int>> CancelOrder(int OrderId)
        {
            // 取得訂單資料 (只能取自己的訂單)
            var oldOrder = await _context.Orders.Include(o => o.Orderdetail)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId && o.UserId == userId);

            if (oldOrder == null)
                return ResultDTO<int>.Failure("找不到該訂單");

            if (oldOrder.Status == "已取消")
                return ResultDTO<int>.Failure("訂單已取消");

            // 已出貨 / 已完成 / 其他 狀態都不可由會員取消 (避免出貨後仍退點)
            if (oldOrder.Status != EditableStatus)
                return ResultDTO<int>.Failure($"訂單狀態為「{oldOrder.Status}」，不能取消");

            // 取得相關商品
            var productIds = oldOrder.Orderdetail.Select(d => d.ProductId).Distinct().ToList();
            var productDict = await _context.Product
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // 開啟交易
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await _orderService.CancelOrderAsync(oldOrder, productDict);


            if (result.Ok)
            {
                await transaction.CommitAsync();
                return result;
            }
            else
            {
                await transaction.RollbackAsync();
                return result;
            }
        }
    }
}
