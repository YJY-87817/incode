using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.PointShop
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly incodedatabaseContext _context;
        private readonly IOrderService _orders;

        public AdminOrderService(incodedatabaseContext context, IOrderService orders)
        {
            _context = context;
            _orders = orders;
        }

        public Task<List<Orders>> GetOrdersAsync() => _context.Orders.AsNoTracking().ToListAsync();

        public async Task<OrderViewModel?> GetOrderForEditAsync(int orderId)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return null;

            return new OrderViewModel
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                TotalPoint = order.TotalPoint,
                Status = order.Status,
                Remark = order.Remark,
                orderdetails = await _context.Orderdetail.AsNoTracking().Where(od => od.OrderId == orderId).ToListAsync(),
            };
        }

        public Task<List<Product>> GetProductOptionsAsync()
            => _context.Product.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

        // 表單明細只取商品與數量
        private static List<OrderItemRequest> ToItems(IEnumerable<Orderdetail>? details)
            => (details ?? Enumerable.Empty<Orderdetail>())
                .Select(d => new OrderItemRequest { ProductId = d.ProductId, Quantity = d.Quantity })
                .ToList();

        public async Task<ResultDTO<int>> CreateOrderAsync(OrderViewModel form)
        {
            if (!await _context.User.AnyAsync(u => u.UserId == form.UserId))
                return ResultDTO<int>.Failure($"找不到會員 {form.UserId}", 404);

            return await _orders.CreateOrderAsync(
                form.UserId,
                ToItems(form.orderdetails),
                recipient: null,               // 寄送資料由出貨管理另外建立
                remark: form.Remark,
                requireRecipient: false,
                status: form.Status ?? OrderStatus.Pending);
        }

        public async Task<ResultDTO<int>> UpdateOrderAsync(int orderId, OrderViewModel form)
        {
            var currentStatus = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId).Select(o => o.Status).FirstOrDefaultAsync();
            if (currentStatus == null)
                return ResultDTO<int>.Failure("找不到該訂單", 404);

            if (!OrderStatus.IsValid(form.Status))
                return ResultDTO<int>.Failure("訂單狀態錯誤");

            // 已取消的訂單庫存與點數都已退還，不可改回其他狀態 (否則等於免費取得商品)
            if (currentStatus == OrderStatus.Canceled)
            {
                if (form.Status != OrderStatus.Canceled)
                    return ResultDTO<int>.Failure("已取消的訂單不能恢復，請重新建立訂單", 409);

                await UpdateRemarkAsync(orderId, form.Remark);
                return ResultDTO<int>.Success(orderId, "訂單備註已更新！");
            }

            // 改為取消：退還庫存與點數 (會員 / 明細不允許在此同時修改)
            if (form.Status == OrderStatus.Canceled)
            {
                var cancel = await _orders.CancelOrderAsync(orderId);
                if (!cancel.Ok) return KeepFormError(cancel);

                await UpdateRemarkAsync(orderId, form.Remark);
                return ResultDTO<int>.Success(orderId, "訂單已取消，庫存與點數已退還！");
            }

            // 修改明細與狀態：庫存、總點數、點數差額、狀態都在同一個交易（鎖內）完成。
            // 原本狀態是明細修改完成之後才另外更新：期間被取消的訂單會被改回「已出貨」，庫存與點數卻已退還
            var result = await _orders.UpdateOrderItemsAsync(
                orderId, ToItems(form.orderdetails), form.Remark ?? "", requiredStatus: null, newStatus: form.Status);
            if (!result.Ok) return KeepFormError(result);

            return ResultDTO<int>.Success(orderId, "訂單修改成功！");
        }

        // 訂單已確認存在：之後出現的 404（例如明細的商品不存在「無此商品 5」）是表單輸入錯誤，改回 400，
        // 呼叫端才不會把它當成「找不到訂單」顯示空白的 404 頁
        private static ResultDTO<int> KeepFormError(ResultDTO<int> failure)
            => failure.Code == 404 ? ResultDTO<int>.Failure(failure.Message ?? "訂單修改失敗", 400) : failure;

        private Task UpdateRemarkAsync(int orderId, string? remark)
            => _context.Orders
                .Where(o => o.OrderId == orderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Remark, remark ?? ""));
    }
}
