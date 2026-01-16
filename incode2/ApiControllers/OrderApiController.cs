using incode2.DTO;
using incode2.DTO.PointShop;
using incode2.Models;
using incode2.Partials;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.ApiControllers
{
    [EnableCors("VueApp")]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public OrderApiController(incodedatabaseContext context)
        {
            _context = context;
        }

        // 取得UserId的訂單資料
        // GET: api/OrderApi/order/{UserId}
        [HttpGet("order/{UserId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrder(int userId)
        {
            // 1. 使用 ToListAsync 進行非同步查詢
            var orders = await _context.Orders
                .Where(e => e.UserId == userId)
                .Select(e => new OrderDTO
                {
                    UserId = e.UserId,
                    TotalPoint = e.TotalPoint,
                    Remark = e.Remark,
                    Status = e.Status,
                    OrderId = e.OrderId
                })
                .ToListAsync();

            // 2. 判斷是否找到資料 (選用：若使用者沒訂單回傳空陣列是合理的，若要嚴格檢查可用以下判斷)
            if (orders == null)
            {
                return NotFound();
            }

            return Ok(orders);
        }

        // 取得OrderId的訂單資料
        // GET: api/OrderApi/orderdetail/{OrderId}
        [HttpGet("orderdetail/{OrderId}")]
        public async Task<ActionResult<IEnumerable<OrderDetailDTO>>> GetOrderDetail(int orderId)
        {
            // 1. 執行非同步查詢
            // 2. 移除 .Include()，因為 .Select() 會自動處理 Join
            var details = await _context.Orderdetail
                .Where(e => e.OrderId == orderId)
                .Select(e => new OrderDetailDTO
                {
                    OrderdetailId = e.OrderdetailId,
                    OrderId = e.OrderId,
                    ProductId = e.ProductId,
                    ProductName = e.Product.Name,     // 自動導航至 Product 資料表
                    ImageName = e.Product.ImageName, // 自動導航至 Product 資料表
                    Quantity = e.Quantity,
                    UnitPointAtPurchase = e.UnitPointAtPurchase,
                    RequiresShipping = e.RequiresShipping
                })
                .ToListAsync();

            // 3. 檢查是否有資料
            if (details == null || !details.Any())
            {
                return NotFound($"找不到訂單編號為 {orderId} 的明細資料");
            }

            return Ok(details);
        }

        // 取得ShippedDetailId的訂單資料
        // GET: api/OrderApi/shippeddetail/{OrderdetailId}
        [HttpGet("shippeddetail/{OrderdetailId}")]
        public async Task<ActionResult<IEnumerable<ShippedDTO>>> GetShippedDetail(int orderDetailId)
        {
            var results = await _context.ShippedDetail
        .Where(e => e.OrderdetailId == orderDetailId)
        .Select(e => new ShippedDTO
        {
            ShippedDate = e.ShippedDate,
            ArrivalDate = e.ArrivalDate,
            IsCompleted = e.IsCompleted,
            IsFailed = e.IsFailed,
            Remark = e.Remark,
            OrderdetailId = e.OrderdetailId,
            ProductId = e.Orderdetail.ProductId,
            ProductName = e.Orderdetail.Product.Name
        })
        .ToListAsync();

            if (results == null || !results.Any())
            {
                return NotFound($"找不到訂單明細 ID 為 {orderDetailId} 的出貨資料");
            }

            return Ok(results);
        }

        // 新增訂單
        // POST: api/OrderApi/OrderPost
        [HttpPost("OrderPost")]
        public async Task<ResultDTO<int>> CreateOrder([FromBody] CreateOrderRequest Order)
        {
            if (Order == null || Order.Items == null || !Order.Items.Any())
                return ResultDTO<int>.Failure("無效的請求數據");

            // 檢查 user 是否存在
            var user = await _context.User.FindAsync(Order.UserId);
            if (user == null)
                return ResultDTO<int>.Failure($"找不到該會員{user}");

            // 取得所有商品
            var productIds = Order.Items.Select(i => i.ProductId).Distinct().ToList();
            var productDict = await _context.Product
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // 檢查商品是否都存在
            foreach (var item in Order.Items)
            {
                if (!productDict.TryGetValue(item.ProductId, out var prod))
                    return ResultDTO<int>.Failure($"無此商品{item.ProductId}");

                if (prod.Stock < item.Quantity)
                    return ResultDTO<int>.Failure($"商品 {prod.Name} 庫存不足" );
            }

            // 交易建立訂單與明細
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 建立訂單主檔
                var order = new Orders
                {
                    UserId = Order.UserId,
                    Remark = Order.Remark ?? "",
                    Status = "Pending", // 建議使用更正式的狀態
                    OrderDate = DateTime.Now,
                    TotalPoint = 0,
                };

                int totalPoints = 0;

                // 5. 處理明細與庫存扣除
                foreach (var item in Order.Items)
                {
                    var prod = productDict[item.ProductId];

                    // 建立明細 (不需要手動填 OrderId，EF 會自動關聯)
                    var detail = new Orderdetail
                    {
                        ProductId = prod.ProductId,
                        Quantity = item.Quantity,
                        UnitPointAtPurchase = prod.Point,
                        RequiresShipping = prod.IsPhysical
                    };

                    order.Orderdetail.Add(detail);

                    // 扣除庫存
                    prod.Stock -= item.Quantity;

                    totalPoints += item.Quantity * prod.Point;
                }

                order.TotalPoint = totalPoints;
                _context.Orders.Update(order);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ResultDTO<int>.Success(order.OrderId, "訂單建立成功");
            }
            catch
            {
                await transaction.RollbackAsync();
                return ResultDTO<int>.Failure($"伺服器內部錯誤", 500);
            }
        }
    }
}
