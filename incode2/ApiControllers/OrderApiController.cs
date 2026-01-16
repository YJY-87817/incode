using incode2.DTO;
using incode2.DTO.PointShop;
using incode2.Models;
using incode2.Partials;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
                return NotFound($"找不到會員ID為 {userId} 的訂單資料");
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
                return NotFound($"找不到訂單編號為 {orderId} 的資料明細");
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
                return NotFound($"找不到訂單明細ID為 {orderDetailId} 的出貨資料");
            }

            return Ok(results);
        }


        // 新增訂單
        // POST: api/OrderApi/OrderPost
        [HttpPost("OrderPost")]
        public async Task<ResultDTO<int>> CreateOrder([FromBody] OrderRequest Order)
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
                    return ResultDTO<int>.Failure($"商品 {prod.Name} 庫存不足");
            }

            // 交易建立訂單與明細
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

                return ResultDTO<int>.Success(order.OrderId, "訂單建立成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }


        // 新增寄送資料
        // POST: api/OrderApi/ShippedPost
        [HttpPost("ShippedPost")]
        public async Task<ResultDTO<int>> CreateShipped([FromBody] NewShippedRequest Shipped)
        {
            // 檢驗是否空
            if (Shipped == null)
                return ResultDTO<int>.Failure("無效的請求數據");

            // 檢驗ID是否存在
            var orderdetail = await _context.User.FindAsync(Shipped.OrderdetailId);
            if (orderdetail == null)
                return ResultDTO<int>.Failure($"找不到該訂單細項{orderdetail}");

            // 檢查寄送資料
            if (Shipped.RecipientName == "" || Shipped.RecipientPhone == "" || Shipped.RecipientAddress == "")
                return ResultDTO<int>.Failure($"寄送資料不完整");

            // 存入資料
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 建立訂單主檔
                var shipped = new ShippedDetail
                {
                    OrderdetailId = Shipped.OrderdetailId,
                    RecipientName = Shipped.RecipientName,
                    RecipientAddress = Shipped.RecipientAddress,
                    RecipientPhone = Shipped.RecipientPhone,
                    IsFailed = false,
                    Remark = Shipped.Remark ?? "",
                    IsCompleted = false,
                    ShippedDate = null,
                    ArrivalDate = null,
                };

                _context.ShippedDetail.Update(shipped);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ResultDTO<int>.Success(shipped.ShippedDetailId, "寄送資料建立成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }


        // 修改寄送資料
        // PUT: api/OrderApi/ShippedPut/{ShippedDetailId}
        [HttpPut("ShippedPut/{ShippedDetailId}")]
        public async Task<ResultDTO<int>> EditShipped(int ShippedDetailId,
        [FromBody] NewShippedRequest Shipped)
        {
            // 檢驗是否空
            if (Shipped == null)
                return ResultDTO<int>.Failure("無效的請求數據");

            // 找出資料庫中現有的資料
            var existingShipped = await _context.ShippedDetail.FindAsync(ShippedDetailId);

            if (existingShipped == null)
                return ResultDTO<int>.Failure($"找不到編號為 {ShippedDetailId} 的寄送資料");

            // 檢查邏輯 (選填：例如已完成的訂單不可修改)
            if (existingShipped.IsCompleted)
                return ResultDTO<int>.Failure("已完成的寄送資料不可修改");

            // 更新資料
            try
            {
                // 更新欄位 (將新資料蓋過舊資料)
                existingShipped.RecipientName = Shipped.RecipientName;
                existingShipped.RecipientPhone = Shipped.RecipientPhone;
                existingShipped.RecipientAddress = Shipped.RecipientAddress;
                existingShipped.Remark = Shipped.Remark ?? existingShipped.Remark;

                // 儲存變更 (EF Core 會自動追蹤 existingShipped 的變化)
                await _context.SaveChangesAsync();

                return ResultDTO<int>.Success(existingShipped.ShippedDetailId, "寄送資料修改成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }


        // 修改訂單資料
        // PUT: api/OrderApi/OrderPut/{OrderId}
        [HttpPut("OrderPut/{OrderId}")]
        public async Task<ResultDTO<int>> EditOrder(int OrderId, [FromBody] OrderRequest Order)
        {
            // 取得修改前訂單資料
            var oldOrder = await _context.Orders.Include(o => o.Orderdetail).FirstOrDefaultAsync(o => o.OrderId == OrderId);

            if (oldOrder == null)
                return ResultDTO<int>.Failure("找不到該訂單");

            // 開啟交易(一個存失敗全部不做)
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 更新備註
                oldOrder.Remark = Order.Remark ?? "";

                // 處理明細同步
                // 取得前端傳來的 ProductId 集合
                var productsdto = Order.Items.Select(i => i.ProductId).ToList();
                var productDict = await _context.Product.Where(p => productsdto.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

                // A. 刪除：找出資料庫有但前端沒傳過來的項目
                var itemsToRemove = oldOrder.Orderdetail
                    .Where(d => !productsdto.Contains(d.ProductId)).ToList();

                foreach (var removeItem in itemsToRemove)
                {
                    // 歸還庫存
                    var prod = await _context.Product.FindAsync(removeItem.ProductId);
                    if (prod != null) prod.Stock += removeItem.Quantity;

                    _context.Orderdetail.Remove(removeItem);
                }

                // B. 更新與新增
                int totalPoints = 0;
                foreach (var itemDto in Order.Items)
                {
                    if (!productDict.TryGetValue(itemDto.ProductId, out var prod))
                        return ResultDTO<int>.Failure($"找不到商品 {itemDto.ProductId}");

                    var oldDetail = oldOrder.Orderdetail
                        .FirstOrDefault(d => d.ProductId == itemDto.ProductId);

                    if (oldDetail != null)
                    {
                        // --- 更新舊項目 ---
                        // 處理庫存數量加減
                        int diff = itemDto.Quantity - oldDetail.Quantity;
                        if (prod.Stock < diff)
                            return ResultDTO<int>.Failure($"{prod.Name} 庫存不足");

                        prod.Stock -= diff;
                        oldDetail.Quantity = itemDto.Quantity;
                        oldDetail.UnitPointAtPurchase = prod.Point; // 更新當時點數
                    }
                    else
                    {
                        // --- 新增項目 ---
                        if (prod.Stock < itemDto.Quantity)
                            return ResultDTO<int>.Failure($"{prod.Name} 庫存不足");

                        prod.Stock -= itemDto.Quantity;
                        oldOrder.Orderdetail.Add(new Orderdetail
                        {
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            UnitPointAtPurchase = prod.Point,
                            RequiresShipping = prod.IsPhysical
                        });
                    }
                    totalPoints += itemDto.Quantity * prod.Point;
                }

                oldOrder.TotalPoint = totalPoints;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // 關閉交易
                return ResultDTO<int>.Success(oldOrder.OrderId, "訂單更新成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }
    }
}
