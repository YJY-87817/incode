using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IOrderService _orderService;

        public OrderController(incodedatabaseContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        // GET: Order
        [HttpGet]
        public ActionResult Index()
        {
            return View(_context.Orders.AsNoTracking());
        }

        // GET: Order/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            // 從資料庫抓出商品（可加條件例如 OnSale、Stock > 0）
            var products = _context.Product.OrderBy(p => p.Name).ToList();
            ViewBag.Products = products;
            return View();
        }

        // POST: Order/Create 新增訂單
        // 總點數、單價、是否需寄送一律由後端依商品資料計算，不採用表單傳入的值
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel NewOrder)
        {
            var userExists = await _context.User.AnyAsync(u => u.UserId == NewOrder.UserId);
            if (!userExists)
                return CreateFailed(NewOrder, $"找不到會員 {NewOrder.UserId}");

            var result = await _orderService.CreateOrderAsync(
                NewOrder.UserId,
                ToItems(NewOrder.orderdetails),
                recipient: null,               // 寄送資料由出貨管理另外建立
                remark: NewOrder.Remark,
                requireRecipient: false,
                status: NewOrder.Status ?? OrderStatus.Pending);

            if (!result.Ok)
                return CreateFailed(NewOrder, result.Message);

            TempData["SuccessMessage"] = "訂單建立成功！";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult CreateFailed(OrderViewModel model, string? message)
        {
            TempData["ErrorMessage"] = message ?? "訂單建立失敗！";
            ViewBag.Products = _context.Product.OrderBy(p => p.Name).ToList();
            return View(nameof(Create), model);
        }

        // 表單明細只取商品與數量
        private static List<OrderItemRequest> ToItems(IEnumerable<Orderdetail>? details)
            => (details ?? Enumerable.Empty<Orderdetail>())
                .Select(d => new OrderItemRequest { ProductId = d.ProductId, Quantity = d.Quantity })
                .ToList();

        // GET: Order/Edit/5 顯示頁面
        [Route("/Order/Edit/{OrderId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int OrderId)
        {
            var Order = await _context.Orders.FindAsync(OrderId);
            if (Order == null)
            {
                return NotFound();
            }

            var orderdetail = new OrderViewModel();
            orderdetail.OrderId = Order.OrderId; // 記得加上這一行，表單提交才抓得到 ID
            orderdetail.UserId = Order.UserId;
            orderdetail.TotalPoint = Order.TotalPoint;
            orderdetail.Status = Order.Status;
            orderdetail.Remark = Order.Remark;

            orderdetail.orderdetails = await _context.Orderdetail
                .Where(od => od.OrderId == OrderId)
                .ToListAsync();

            var products = _context.Product.OrderBy(p => p.Name).ToList();
            ViewBag.Products = products;
            return View(orderdetail);
        }

        // POST: Order/Edit/5 修改訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel EditOrder, int OrderId)
        {
            if (OrderId != EditOrder.OrderId)
                return NotFound();

            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == OrderId);
            if (order == null)
                return NotFound();

            if (!OrderStatus.IsValid(EditOrder.Status))
                return EditFailed(EditOrder, "訂單狀態錯誤");

            // 已取消的訂單庫存與點數都已退還，不可改回其他狀態 (否則等於免費取得商品)
            if (order.Status == OrderStatus.Canceled)
            {
                if (EditOrder.Status != OrderStatus.Canceled)
                    return EditFailed(EditOrder, "已取消的訂單不能恢復，請重新建立訂單");

                await UpdateRemarkAsync(OrderId, EditOrder.Remark);
                TempData["SuccessMessage"] = "訂單備註已更新！";
                return RedirectToAction(nameof(Index));
            }

            // 改為取消：退還庫存與點數 (會員 / 明細不允許在此同時修改)
            if (EditOrder.Status == OrderStatus.Canceled)
            {
                var cancel = await _orderService.CancelOrderAsync(OrderId);
                if (!cancel.Ok)
                    return EditFailed(EditOrder, cancel.Message);

                await UpdateRemarkAsync(OrderId, EditOrder.Remark);
                TempData["SuccessMessage"] = "訂單已取消，庫存與點數已退還！";
                return RedirectToAction(nameof(Index));
            }

            // 修改明細：庫存、總點數、點數差額由 Service 在交易內處理 (會員不可變更)
            var result = await _orderService.UpdateOrderItemsAsync(
                OrderId, ToItems(EditOrder.orderdetails), EditOrder.Remark ?? "");
            if (!result.Ok)
                return EditFailed(EditOrder, result.Message);

            await _context.Orders
                .Where(o => o.OrderId == OrderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, EditOrder.Status));

            TempData["SuccessMessage"] = "訂單修改成功！";
            return RedirectToAction(nameof(Index));
        }

        private Task UpdateRemarkAsync(int orderId, string? remark)
            => _context.Orders
                .Where(o => o.OrderId == orderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Remark, remark ?? ""));

        private IActionResult EditFailed(OrderViewModel model, string? message)
        {
            TempData["ErrorMessage"] = message ?? "訂單修改失敗！";
            ViewBag.Products = _context.Product.OrderBy(p => p.Name).ToList();
            return View(nameof(Edit), model);
        }

        // GET: Order/GetShippedData - 提供寄送資料的 API
        [HttpGet]
        public JsonResult GetShippedData()
        {
            var shippedData = _context.ShippedDetail
                .Include(e => e.Order)
                .Select(item => new
                {
                    shippedDetailId = item.ShippedDetailId,
                    orderId = item.OrderId,
                    orderStatus = item.Order.Status,
                    recipientName = item.RecipientName,
                    recipientAddress = item.RecipientAddress,
                    recipientPhone = item.RecipientPhone,
                    shippedDate = item.ShippedDate.HasValue ? item.ShippedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    shippedDateFormatted = item.ShippedDate.HasValue ? item.ShippedDate.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    arrivalDate = item.ArrivalDate.HasValue ? item.ArrivalDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    arrivalDateFormatted = item.ArrivalDate.HasValue ? item.ArrivalDate.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    isFailed = item.IsFailed,
                    remark = item.Remark,
                    isCompleted = item.IsCompleted
                })
                .ToList();

            return Json(shippedData);
        }
    }
}
