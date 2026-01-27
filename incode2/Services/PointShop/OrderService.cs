using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.PointShop
{
    public class OrderService : IOrderService
    {
        private readonly incodedatabaseContext _context;

        public OrderService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 取得UserId的訂單資料
        public async Task<ResultDTO<IEnumerable<OrderDTO>>> GetOrderAsync(int userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(e => e.UserId == userId)
                    .Select(e => new OrderDTO
                    {
                        UserId = e.UserId,
                        TotalPoint = e.TotalPoint,
                        Remark = e.Remark,
                        OrderDate = e.OrderDate,
                        Status = e.Status,
                        OrderId = e.OrderId
                    })
                    .ToListAsync();

                return ResultDTO<IEnumerable<OrderDTO>>.Success(orders);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<OrderDTO>>.Failure($"伺服器內部錯誤: {ex.Message}", 500);
            }
        }

        // 取得OrderId的訂單資料
        public async Task<ResultDTO<IEnumerable<OrderDetailDTO>>> GetOrderDetailAsync(int orderId)
        {
            try
            {
                var details = await _context.Orderdetail
                    .Where(e => e.OrderId == orderId)
                    .Select(e => new OrderDetailDTO
                    {
                        OrderdetailId = e.OrderdetailId,
                        OrderId = e.OrderId,
                        ProductId = e.ProductId,
                        ProductName = e.Product.Name,
                        ImageName = e.Product.ImageName,
                        Quantity = e.Quantity,
                        UnitPointAtPurchase = e.UnitPointAtPurchase,
                        RequiresShipping = e.RequiresShipping
                    })
                    .ToListAsync();

                if (details == null || !details.Any())
                    return ResultDTO<IEnumerable<OrderDetailDTO>>.Failure($"找不到訂單編號為 {orderId} 的資料明細", 404);

                return ResultDTO<IEnumerable<OrderDetailDTO>>.Success(details);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<OrderDetailDTO>>.Failure($"伺服器內部錯誤: {ex.Message}", 500);
            }
        }

        // 取得ShippedDetailId的訂單資料
        public async Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetailAsync(int orderDetailId)
        {
            try
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
                    return ResultDTO<IEnumerable<ShippedDTO>>.Failure($"找不到訂單明細ID為 {orderDetailId} 的出貨資料", 404);

                return ResultDTO<IEnumerable<ShippedDTO>>.Success(results);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<ShippedDTO>>.Failure($"伺服器內部錯誤: {ex.Message}", 500);
            }
        }

        // 新增訂單
        public async Task<ResultDTO<int>> CreateOrderAsync(OrderRequest order)
        {
            if (order == null || order.Items == null || !order.Items.Any())
                return ResultDTO<int>.Failure("無效的請求數據");

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

            // 交易建立訂單與明細
            try
            {
                // 建立訂單主檔
                var newOrder = new Orders
                {
                    UserId = order.UserId,
                    Remark = order.Remark ?? "",
                    Status = "Pending",
                    OrderDate = DateTime.Now,
                    TotalPoint = 0,
                };

                int totalPoints = 0;

                // 處理明細與庫存扣除
                foreach (var item in order.Items)
                {
                    var prod = productDict[item.ProductId];

                    // 建立明細
                    var detail = new Orderdetail
                    {
                        ProductId = prod.ProductId,
                        Quantity = item.Quantity,
                        UnitPointAtPurchase = prod.Point,
                        RequiresShipping = prod.IsPhysical
                    };

                    newOrder.Orderdetail.Add(detail);

                    // 扣除庫存
                    prod.Stock -= item.Quantity;

                    totalPoints += item.Quantity * prod.Point;
                }

                newOrder.TotalPoint = totalPoints;
                _context.Orders.Add(newOrder);

                await _context.SaveChangesAsync();

                return ResultDTO<int>.Success(newOrder.OrderId, "訂單建立成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 新增寄送資料
        public async Task<ResultDTO<int>> CreateShippedAsync(NewShippedRequest shipped)
        {
            // 檢驗是否空
            if (shipped == null)
                return ResultDTO<int>.Failure("無效的請求數據");

            // 檢驗ID是否存在
            var orderdetail = await _context.Orderdetail.FindAsync(shipped.OrderdetailId);
            if (orderdetail == null)
                return ResultDTO<int>.Failure($"找不到該訂單細項{shipped.OrderdetailId}");

            // 檢查寄送資料
            if (string.IsNullOrWhiteSpace(shipped.RecipientName) || 
                string.IsNullOrWhiteSpace(shipped.RecipientPhone) || 
                string.IsNullOrWhiteSpace(shipped.RecipientAddress))
                return ResultDTO<int>.Failure($"寄送資料不完整");

            // 存入資料
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 建立寄送資料
                var newShipped = new ShippedDetail
                {
                    OrderdetailId = shipped.OrderdetailId,
                    RecipientName = shipped.RecipientName,
                    RecipientAddress = shipped.RecipientAddress,
                    RecipientPhone = shipped.RecipientPhone,
                    IsFailed = false,
                    Remark = shipped.Remark ?? "",
                    IsCompleted = false,
                    ShippedDate = null,
                    ArrivalDate = null,
                };

                _context.ShippedDetail.Add(newShipped);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ResultDTO<int>.Success(newShipped.ShippedDetailId, "寄送資料建立成功");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 修改寄送資料
        public async Task<ResultDTO<int>> EditShippedAsync(int shippedDetailId, NewShippedRequest shipped)
        {
            // 檢驗是否空
            if (shipped == null)
                return ResultDTO<int>.Failure("無效的請求數據");

            // 找出資料庫中現有的資料
            var existingShipped = await _context.ShippedDetail.FindAsync(shippedDetailId);

            if (existingShipped == null)
                return ResultDTO<int>.Failure($"找不到編號為 {shippedDetailId} 的寄送資料");

            // 檢查邏輯
            if (existingShipped.IsCompleted)
                return ResultDTO<int>.Failure("已完成的寄送資料不可修改");

            // 更新資料
            try
            {
                // 更新欄位
                existingShipped.RecipientName = shipped.RecipientName;
                existingShipped.RecipientPhone = shipped.RecipientPhone;
                existingShipped.RecipientAddress = shipped.RecipientAddress;
                existingShipped.Remark = shipped.Remark ?? existingShipped.Remark;

                // 儲存變更
                await _context.SaveChangesAsync();

                return ResultDTO<int>.Success(existingShipped.ShippedDetailId, "寄送資料修改成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 修改訂單資料
        public async Task<ResultDTO<int>> EditOrderAsync(int orderId, OrderRequest order)
        {
            // 取得修改前訂單資料
            var oldOrder = await _context.Orders.Include(o => o.Orderdetail).FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (oldOrder == null)
                return ResultDTO<int>.Failure("找不到該訂單");

            if (oldOrder.Status == "已完成")
                return ResultDTO<int>.Failure("訂單已完成不能修改");

            // 開啟交易
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 更新備註
                oldOrder.Remark = order.Remark ?? "";

                // 處理明細同步
                var productsdto = order.Items.Select(i => i.ProductId).ToList();
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
                foreach (var itemDto in order.Items)
                {
                    if (!productDict.TryGetValue(itemDto.ProductId, out var prod))
                        return ResultDTO<int>.Failure($"找不到商品 {itemDto.ProductId}");

                    var oldDetail = oldOrder.Orderdetail
                        .FirstOrDefault(d => d.ProductId == itemDto.ProductId);

                    if (oldDetail != null)
                    {
                        // 更新舊項目
                        int diff = itemDto.Quantity - oldDetail.Quantity;
                        if (prod.Stock < diff)
                            return ResultDTO<int>.Failure($"{prod.Name} 庫存不足");

                        prod.Stock -= diff;
                        oldDetail.Quantity = itemDto.Quantity;
                        oldDetail.UnitPointAtPurchase = prod.Point;
                    }
                    else
                    {
                        // 新增項目
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
                await transaction.CommitAsync();
                return ResultDTO<int>.Success(oldOrder.OrderId, "訂單更新成功");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 取消訂單
        public async Task<ResultDTO<int>> CancelOrderAsync(int orderId)
        {
            // 取得修改前訂單資料
            var oldOrder = await _context.Orders.Include(o => o.Orderdetail).FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (oldOrder == null)
                return ResultDTO<int>.Failure("找不到該訂單");

            if (oldOrder.Status == "已完成")
                return ResultDTO<int>.Failure("訂單已完成不能取消");

            if (oldOrder.Status == "取消")
                return ResultDTO<int>.Failure("訂單已取消");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 回復庫存
                var productIds = oldOrder.Orderdetail.Select(d => d.ProductId).Distinct().ToList();
                var productDict = await _context.Product
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var detail in oldOrder.Orderdetail)
                {
                    if (productDict.TryGetValue(detail.ProductId, out var prod))
                    {
                        prod.Stock += detail.Quantity;
                    }
                }

                oldOrder.Status = "取消";
                oldOrder.CanceledAt = DateTime.Now;

                // 儲存變更
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ResultDTO<int>.Success(oldOrder.OrderId, "訂單取消並回復庫存/點數成功");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }
    }
}
