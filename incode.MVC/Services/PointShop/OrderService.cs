using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.PointShop
{
    public class OrderService : IOrderService
    {
        private const string ServerErrorMessage = "伺服器內部錯誤，請稍後再試";

        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointlogService;
        private readonly IMissionService _missionService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(incodedatabaseContext context, IPointLogService pointLogService,
            IMissionService missionService, ILogger<OrderService> logger)
        {
            _context = context;
            _pointlogService = pointLogService;
            _missionService = missionService;
            _logger = logger;
        }

        // 交易內的業務失敗 (庫存 / 點數不足等)，丟出後整筆 rollback 並回傳訊息給呼叫端
        private sealed class OrderFailure : Exception
        {
            public int Code { get; }
            public OrderFailure(string message, int code = 400) : base(message) => Code = code;
        }

        // ======================================================================
        // 查詢
        // ======================================================================

        public Task<Orders?> GetOwnOrderAsync(int orderId, int userId)
            => _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        public Task<Orders?> GetOwnOrderByShippedDetailAsync(int shippedDetailId, int userId)
            => _context.ShippedDetail
                .AsNoTracking()
                .Where(s => s.ShippedDetailId == shippedDetailId && s.Order.UserId == userId)
                .Select(s => s.Order)
                .FirstOrDefaultAsync();

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
                    }).OrderByDescending(e => e.OrderId)
                    .ToListAsync();

                return ResultDTO<IEnumerable<OrderDTO>>.Success(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得會員 {UserId} 訂單失敗", userId);
                return ResultDTO<IEnumerable<OrderDTO>>.Failure(ServerErrorMessage, 500);
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

                if (!details.Any())
                    return ResultDTO<IEnumerable<OrderDetailDTO>>.Success(details, $"找不到訂單編號為 {orderId} 的資料明細");

                return ResultDTO<IEnumerable<OrderDetailDTO>>.Success(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得訂單 {OrderId} 明細失敗", orderId);
                return ResultDTO<IEnumerable<OrderDetailDTO>>.Failure(ServerErrorMessage, 500);
            }
        }

        // 取得ShippedDetailId的訂單資料
        public async Task<ResultDTO<IEnumerable<ShippedDTO>>> GetShippedDetailAsync(int orderId)
        {
            try
            {
                var results = await _context.ShippedDetail
                    .Where(e => e.OrderId == orderId)
                    .Select(e => new ShippedDTO
                    {
                        ShippedDetailId = e.ShippedDetailId,
                        ShippedDate = e.ShippedDate,
                        ArrivalDate = e.ArrivalDate,
                        IsCompleted = e.IsCompleted,
                        IsFailed = e.IsFailed,
                        Remark = e.Remark,
                        OrderId = e.OrderId,
                        MaskedName = ShippedDTO.MaskName(e.RecipientName),
                        MaskedPhone = ShippedDTO.MaskPhone(e.RecipientPhone),
                        MaskedAddress = ShippedDTO.MaskAddress(e.RecipientAddress)
                    })
                    .ToListAsync();

                if (!results.Any())
                    return ResultDTO<IEnumerable<ShippedDTO>>.Success(results, $"找不到訂單編號為{orderId}的寄送資料");

                return ResultDTO<IEnumerable<ShippedDTO>>.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得訂單 {OrderId} 寄送資料失敗", orderId);
                return ResultDTO<IEnumerable<ShippedDTO>>.Failure(ServerErrorMessage, 500);
            }
        }

        // 取得輸入過寄送資料
        public async Task<ResultDTO<IEnumerable<ShipAddressDTO>>> GetHasShippedDetailAsync(int userId)
        {
            try
            {
                var shipAddresses = await _context.Shipaddress
                    .Where(e => e.UserId == userId)
                    .Select(e => new ShipAddressDTO
                    {
                        RecipientName = e.RecipientName,
                        RecipientPhone = e.RecipientPhone,
                        RecipientAddress = e.RecipientAddress
                    })
                    .ToListAsync();

                return ResultDTO<IEnumerable<ShipAddressDTO>>.Success(shipAddresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得會員 {UserId} 寄送地址失敗", userId);
                return ResultDTO<IEnumerable<ShipAddressDTO>>.Failure(ServerErrorMessage, 500);
            }
        }

        // ======================================================================
        // 異動
        // ======================================================================

        // 新增訂單
        public async Task<ResultDTO<int>> CreateOrderAsync(int userId, IEnumerable<OrderItemRequest>? items,
            NewShippedRequest? recipient, string? remark, bool requireRecipient, string status = OrderStatus.Pending)
        {
            var (quantities, itemError) = NormalizeItems(items);
            if (itemError != null)
                return ResultDTO<int>.Failure(itemError);

            if (!OrderStatus.IsValid(status) || status == OrderStatus.Canceled)
                return ResultDTO<int>.Failure("訂單狀態錯誤");

            var products = await LoadProductsAsync(quantities.Keys);
            var productError = CheckProducts(quantities.Keys, products, mustBeOnSale: quantities.Keys);
            if (productError != null)
                return productError;

            var needsShipping = quantities.Keys.Any(id => products[id].IsPhysical);
            var hasRecipient = recipient != null
                && !string.IsNullOrWhiteSpace(recipient.RecipientName)
                && !string.IsNullOrWhiteSpace(recipient.RecipientPhone)
                && !string.IsNullOrWhiteSpace(recipient.RecipientAddress);

            if (needsShipping && requireRecipient && !hasRecipient)
                return ResultDTO<int>.Failure("訂單含實體商品，請填寫完整寄送資料");

            var result = await RunInTransactionAsync(userId, "新增訂單", async () =>
            {
                // 1. 原子扣庫存 (UPDATE ... WHERE Stock >= 數量)，避免同時下單超賣
                foreach (var (productId, qty) in quantities)
                    await DecreaseStockAsync(products[productId], qty);

                // 2. 建立訂單與明細 (價格以資料庫為準)
                var order = new Orders
                {
                    UserId = userId,
                    Remark = remark ?? "",
                    Status = status,
                    OrderDate = DateTime.Now,
                };

                foreach (var (productId, qty) in quantities)
                {
                    var product = products[productId];
                    order.Orderdetail.Add(new Orderdetail
                    {
                        ProductId = productId,
                        Quantity = qty,
                        UnitPointAtPurchase = product.Point,
                        RequiresShipping = product.IsPhysical
                    });
                    order.TotalPoint += qty * product.Point;
                }

                // 3. 寄送資料
                if (needsShipping && hasRecipient)
                {
                    var name = recipient!.RecipientName.Trim();
                    var phone = recipient.RecipientPhone.Trim();
                    var address = recipient.RecipientAddress.Trim();

                    order.ShippedDetail.Add(new ShippedDetail
                    {
                        RecipientName = name,
                        RecipientPhone = phone,
                        RecipientAddress = address,
                        IsFailed = false,
                        IsCompleted = false,
                        Remark = remark ?? ""
                    });

                    // 記住寄送地址 (相同資料不重複新增)
                    var exists = await _context.Shipaddress.AnyAsync(s =>
                        s.UserId == userId &&
                        s.RecipientName == name &&
                        s.RecipientPhone == phone &&
                        s.RecipientAddress == address);
                    if (!exists)
                    {
                        _context.Shipaddress.Add(new Shipaddress
                        {
                            UserId = userId,
                            RecipientName = name,
                            RecipientPhone = phone,
                            RecipientAddress = address,
                        });
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // 取得 OrderId

                // 4. 扣點數
                if (order.TotalPoint > 0)
                {
                    var ok = await _pointlogService.ChangePointsAsync(
                        userId: userId,
                        changeAmount: -order.TotalPoint,
                        sourceType: "order",
                        sourceId: order.OrderId,
                        type: PointType.ExpenseOrders,
                        displaySource: "商品兌換",
                        displaySubTitle: $"訂單編號: {order.OrderId}");
                    if (!ok)
                        throw new OrderFailure("您的點數不足", 409);
                }

                await _context.SaveChangesAsync();
                return order.OrderId;
            }, "訂單建立成功");

            // 平台任務觸發：購買任務 (在交易外執行，任務失敗不影響已成立的訂單)
            if (result.Ok)
            {
                try
                {
                    await _missionService.TriggerMissionAsync(userId, "purchase", 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "訂單 {OrderId} 觸發購買任務失敗", result.Data);
                }
            }

            return result;
        }

        // 修改訂單明細
        public async Task<ResultDTO<int>> UpdateOrderItemsAsync(int orderId, IEnumerable<OrderItemRequest>? items, string? remark)
        {
            var (newQty, itemError) = NormalizeItems(items);
            if (itemError != null)
                return ResultDTO<int>.Failure(itemError);

            var order = await _context.Orders
                .Include(o => o.Orderdetail)
                .Include(o => o.ShippedDetail)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ResultDTO<int>.Failure("找不到該訂單", 404);

            if (order.Status == OrderStatus.Canceled)
                return ResultDTO<int>.Failure("訂單已取消，不能修改", 409);

            // 舊明細依商品加總 (舊資料可能有同商品多筆)
            var oldQty = order.Orderdetail
                .GroupBy(d => d.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Quantity));

            var allIds = newQty.Keys.Union(oldQty.Keys).ToList();
            var products = await LoadProductsAsync(allIds);

            // 新加入的商品必須存在且上架中；原本就有的商品即使下架也可調整數量
            var addedIds = newQty.Keys.Where(id => !oldQty.ContainsKey(id)).ToList();
            var productError = CheckProducts(newQty.Keys, products, mustBeOnSale: addedIds);
            if (productError != null)
                return productError;

            if (addedIds.Any(id => products[id].IsPhysical) && !order.ShippedDetail.Any())
                return ResultDTO<int>.Failure("此訂單沒有寄送資料，無法加入實體商品，請另外下單");

            return await RunInTransactionAsync(order.UserId, $"修改訂單 {orderId}", async () =>
            {
                // 1. 庫存：增加的數量原子扣減，減少的數量歸還
                foreach (var productId in allIds)
                {
                    var delta = newQty.GetValueOrDefault(productId) - oldQty.GetValueOrDefault(productId);
                    if (delta > 0)
                        await DecreaseStockAsync(products[productId], delta);
                    else if (delta < 0)
                        await IncreaseStockAsync(productId, -delta);
                }

                // 2. 同步明細：原有商品保留原購買單價，新商品使用目前價格
                var totalPoints = 0;
                foreach (var productId in allIds)
                {
                    var lines = order.Orderdetail.Where(d => d.ProductId == productId).ToList();
                    var qty = newQty.GetValueOrDefault(productId);

                    if (qty == 0)
                    {
                        _context.Orderdetail.RemoveRange(lines);
                        continue;
                    }

                    if (lines.Count > 0)
                    {
                        var keep = lines[0];
                        keep.Quantity = qty;
                        _context.Orderdetail.RemoveRange(lines.Skip(1));
                        totalPoints += qty * keep.UnitPointAtPurchase;
                    }
                    else
                    {
                        var product = products[productId];
                        order.Orderdetail.Add(new Orderdetail
                        {
                            ProductId = productId,
                            Quantity = qty,
                            UnitPointAtPurchase = product.Point,
                            RequiresShipping = product.IsPhysical
                        });
                        totalPoints += qty * product.Point;
                    }
                }

                // 3. 點數差額：新總點數 > 舊總點數 → 補扣；反之 → 退還
                var pointDiff = totalPoints - order.TotalPoint;
                if (pointDiff != 0)
                {
                    var ok = await _pointlogService.ChangePointsAsync(
                        userId: order.UserId,
                        changeAmount: -pointDiff,
                        sourceType: "order",
                        sourceId: order.OrderId,
                        type: pointDiff > 0 ? PointType.ExpenseOrders : PointType.IncomeOrdersRefund,
                        displaySource: pointDiff > 0 ? "訂單修改補扣" : "訂單修改退款",
                        displaySubTitle: $"訂單編號: {order.OrderId}");
                    if (!ok)
                        throw new OrderFailure("點數不足，無法修改訂單", 409);
                }

                order.TotalPoint = totalPoints;
                if (remark != null)
                    order.Remark = remark;

                await _context.SaveChangesAsync();
                return order.OrderId;
            }, "訂單更新成功");
        }

        // 取消訂單
        public async Task<ResultDTO<int>> CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Orderdetail)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ResultDTO<int>.Failure("找不到該訂單", 404);

            if (order.Status == OrderStatus.Canceled)
                return ResultDTO<int>.Failure("訂單已取消", 409);

            return await RunInTransactionAsync(order.UserId, $"取消訂單 {orderId}", async () =>
            {
                // 回復庫存
                foreach (var group in order.Orderdetail.GroupBy(d => d.ProductId))
                    await IncreaseStockAsync(group.Key, group.Sum(d => d.Quantity));

                order.Status = OrderStatus.Canceled;
                order.CanceledAt = DateTime.Now;

                // 退還點數
                if (order.TotalPoint > 0)
                {
                    await _pointlogService.ChangePointsAsync(
                        userId: order.UserId,
                        changeAmount: order.TotalPoint,
                        sourceType: "order",
                        sourceId: order.OrderId,
                        type: PointType.IncomeOrdersRefund,
                        displaySource: "訂單取消退款",
                        displaySubTitle: $"訂單編號：{order.OrderId}");
                }

                await _context.SaveChangesAsync();
                return order.OrderId;
            }, "訂單取消並回復庫存/點數成功");
        }

        // 修改寄送資料
        public async Task<ResultDTO<int>> EditShippedAsync(int shippedDetailId, NewShippedRequest shipped)
        {
            if (shipped == null
                || string.IsNullOrWhiteSpace(shipped.RecipientName)
                || string.IsNullOrWhiteSpace(shipped.RecipientPhone)
                || string.IsNullOrWhiteSpace(shipped.RecipientAddress))
                return ResultDTO<int>.Failure("請填寫完整寄送資料");

            var existing = await _context.ShippedDetail.FindAsync(shippedDetailId);
            if (existing == null)
                return ResultDTO<int>.Failure($"找不到編號為 {shippedDetailId} 的寄送資料", 404);

            if (existing.IsCompleted)
                return ResultDTO<int>.Failure("已完成的寄送資料不可修改", 409);

            try
            {
                existing.RecipientName = shipped.RecipientName.Trim();
                existing.RecipientPhone = shipped.RecipientPhone.Trim();
                existing.RecipientAddress = shipped.RecipientAddress.Trim();
                existing.Remark = shipped.Remark ?? existing.Remark;

                await _context.SaveChangesAsync();
                return ResultDTO<int>.Success(existing.ShippedDetailId, "寄送資料修改成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改寄送資料 {ShippedDetailId} 失敗", shippedDetailId);
                return ResultDTO<int>.Failure(ServerErrorMessage, 500);
            }
        }

        // ======================================================================
        // 共用
        // ======================================================================

        // 驗證並合併明細：數量必須 > 0，同商品多筆合併為一筆
        private static (Dictionary<int, int> quantities, string? error) NormalizeItems(IEnumerable<OrderItemRequest>? items)
        {
            var list = items?.Where(i => i != null).ToList();
            if (list == null || list.Count == 0)
                return (new(), "訂單資料有誤");

            if (list.Any(i => i.Quantity <= 0))
                return (new(), "商品數量必須大於 0");

            var quantities = list
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            return (quantities, null);
        }

        // 讀取商品 (不追蹤：庫存一律以 ExecuteUpdate 原子更新，避免 SaveChanges 以舊值覆蓋)
        private Task<Dictionary<int, Product>> LoadProductsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            return _context.Product
                .AsNoTracking()
                .Where(p => ids.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);
        }

        // 檢查商品存在、指定的商品需上架中
        private static ResultDTO<int>? CheckProducts(IEnumerable<int> requiredIds,
            Dictionary<int, Product> products, IEnumerable<int> mustBeOnSale)
        {
            foreach (var id in requiredIds)
            {
                if (!products.ContainsKey(id))
                    return ResultDTO<int>.Failure($"無此商品 {id}", 404);
            }
            foreach (var id in mustBeOnSale)
            {
                if (!products[id].OnSale)
                    return ResultDTO<int>.Failure($"商品 {products[id].Name} 已下架");
            }
            return null;
        }

        private async Task DecreaseStockAsync(Product product, int quantity)
        {
            var affected = await _context.Product
                .Where(p => p.ProductId == product.ProductId && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

            if (affected == 0)
                throw new OrderFailure($"商品 {product.Name} 庫存不足", 409);
        }

        private Task IncreaseStockAsync(int productId, int quantity)
        {
            return _context.Product
                .Where(p => p.ProductId == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + quantity));
        }

        // 在交易內執行異動（先鎖定會員點數，同時下單 / 修改時不會以相同餘額重複扣點）：業務失敗 (OrderFailure) 回傳訊息，其他例外記錄 log 並回傳通用訊息
        private async Task<ResultDTO<int>> RunInTransactionAsync(int userId, string operation,
            Func<Task<int>> work, string successMessage)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _pointlogService.LockUsersAsync(userId);
                var id = await work();
                await transaction.CommitAsync();
                return ResultDTO<int>.Success(id, successMessage);
            }
            catch (OrderFailure failure)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return ResultDTO<int>.Failure(failure.Message, failure.Code);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                _logger.LogError(ex, "{Operation} 失敗 (UserId: {UserId})", operation, userId);
                return ResultDTO<int>.Failure(ServerErrorMessage, 500);
            }
        }
    }
}
