using incode2.Constants;
using incode2.Dtos;
using incode2.Dtos.PointShop;
using incode2.Models;
using incode2.Services.Point;
using Microsoft.EntityFrameworkCore;


namespace incode2.Services.PointShop
{
    public class OrderService : IOrderService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointlogService;


        public OrderService(incodedatabaseContext context, IPointLogService pointLogService)
        {
            _context = context;
            _pointlogService = pointLogService;
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
                    }).OrderByDescending(e => e.OrderId)
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
                    return ResultDTO<IEnumerable<OrderDetailDTO>>.Success(details, $"找不到訂單編號為 {orderId} 的資料明細", 204);

                return ResultDTO<IEnumerable<OrderDetailDTO>>.Success(details);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<OrderDetailDTO>>.Failure($"伺服器內部錯誤: {ex.Message}", 500);
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

                if (results == null || !results.Any())
                    return ResultDTO<IEnumerable<ShippedDTO>>.Success(results, $"找不到訂單編號為{orderId}的寄送資料", 204);

                return ResultDTO<IEnumerable<ShippedDTO>>.Success(results);
            }
            catch (Exception ex)
            {
                return ResultDTO<IEnumerable<ShippedDTO>>.Failure($"伺服器內部錯誤: {ex.Message}", 500);
            }
        }

        // 新增訂單
        public async Task<ResultDTO<int>> CreateOrderAsync(OrderRequest order, int totalPoints, Dictionary<int, Product> productDict)
        {
            try
            {
                // 建立訂單主檔
                var newOrder = new Orders
                {
                    UserId = order.UserId,
                    Remark = order.Remark ?? "",
                    Status = "待處理",
                    OrderDate = DateTime.Now,
                    TotalPoint = totalPoints,
                };

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
                }

                if (newOrder.Orderdetail.Any(e => e.RequiresShipping == true))
                {
                    // 建立寄送資料
                    var recipient = new ShippedDetail
                    {
                        RecipientName = order.Recipient.RecipientName,
                        RecipientPhone = order.Recipient.RecipientPhone,
                        RecipientAddress = order.Recipient.RecipientAddress,
                        IsFailed = false,
                        IsCompleted = false,
                        Remark = order.Remark ?? "",
                        ShippedDate = null,
                        ArrivalDate = null,
                    };
                    newOrder.ShippedDetail.Add(recipient);
                }

                _context.Orders.Add(newOrder);

                await _context.SaveChangesAsync();

                // 扣除使用者點數
                var result = await _pointlogService.ChangePointsAsync(newOrder.UserId, -totalPoints, "order", newOrder.OrderId, PointType.Order);

                if (!result)
                {
                    return ResultDTO<int>.Failure("您的點數不足");
                }
                else
                {
                    return ResultDTO<int>.Success(newOrder.OrderId, "訂單建立成功");
                }
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 修改寄送資料
        public async Task<ResultDTO<int>> EditShippedAsync(ShippedDetail existingShipped, NewShippedRequest shipped)
        {
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
        public async Task<ResultDTO<int>> EditOrderAsync(Orders oldOrder, OrderRequest order, Dictionary<int, Product> productDict)
        {
            try
            {
                // 更新備註
                oldOrder.Remark = order.Remark ?? "";

                // 處理明細同步
                var productsdto = order.Items.Select(i => i.ProductId).ToList();

                // A. 刪除：找出資料庫有但前端沒傳過來的項目
                var itemsToRemove = oldOrder.Orderdetail
                    .Where(d => !productsdto.Contains(d.ProductId)).ToList();

                foreach (var removeItem in itemsToRemove)
                {
                    // 歸還庫存
                    if (productDict.TryGetValue(removeItem.ProductId, out var prod))
                    {
                        prod.Stock += removeItem.Quantity;
                    }

                    _context.Orderdetail.Remove(removeItem);
                }

                // B. 更新與新增
                int totalPoints = 0;
                foreach (var itemDto in order.Items)
                {
                    var prod = productDict[itemDto.ProductId];

                    var oldDetail = oldOrder.Orderdetail
                        .FirstOrDefault(d => d.ProductId == itemDto.ProductId);

                    if (oldDetail != null)
                    {
                        // 更新舊項目
                        int diff = itemDto.Quantity - oldDetail.Quantity;
                        prod.Stock -= diff;
                        oldDetail.Quantity = itemDto.Quantity;
                        oldDetail.UnitPointAtPurchase = prod.Point;
                    }
                    else
                    {
                        // 新增項目
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

                return ResultDTO<int>.Success(oldOrder.OrderId, "訂單更新成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }

        // 取消訂單
        public async Task<ResultDTO<int>> CancelOrderAsync(Orders oldOrder, Dictionary<int, Product> productDict)
        {
            try
            {
                // 回復庫存
                foreach (var detail in oldOrder.Orderdetail)
                {
                    if (productDict.TryGetValue(detail.ProductId, out var prod))
                    {
                        prod.Stock += detail.Quantity;
                    }
                }

                oldOrder.Status = "已取消";
                oldOrder.CanceledAt = DateTime.Now;
                // 儲存變更
                await _context.SaveChangesAsync();


                // 扣除使用者點數
                var result = await _pointlogService.ChangePointsAsync(oldOrder.UserId, oldOrder.TotalPoint, "order", oldOrder.OrderId, PointType.Order);

                if (!result)
                {
                    return ResultDTO<int>.Failure($"訂單取消失敗:點數回復出錯");
                }

                return ResultDTO<int>.Success(oldOrder.OrderId, "訂單取消並回復庫存/點數成功");
            }
            catch (Exception ex)
            {
                return ResultDTO<int>.Failure($"伺服器內部錯誤:{ex.Message}", 500);
            }
        }
    }
}
