using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IAdminOrderService _orders;
        private readonly IAdminShippedService _shipped;

        public OrderController(IAdminOrderService orders, IAdminShippedService shipped)
        {
            _orders = orders;
            _shipped = shipped;
        }

        // GET: Order
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await _orders.GetOrdersAsync());
        }

        // GET: Order/Create 顯示頁面
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _orders.GetProductOptionsAsync();
            return View();
        }

        // POST: Order/Create 新增訂單
        // 總點數、單價、是否需寄送一律由後端依商品資料計算，不採用表單傳入的值
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel NewOrder)
        {
            var result = await _orders.CreateOrderAsync(NewOrder);
            if (!result.Ok)
                return await CreateFailedAsync(NewOrder, result.Message);

            TempData["SuccessMessage"] = "訂單建立成功！";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> CreateFailedAsync(OrderViewModel model, string? message)
        {
            TempData["ErrorMessage"] = message ?? "訂單建立失敗！";
            ViewBag.Products = await _orders.GetProductOptionsAsync();
            return View(nameof(Create), model);
        }

        // GET: Order/Edit/5 顯示頁面
        [Route("/Order/Edit/{OrderId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int OrderId)
        {
            var order = await _orders.GetOrderForEditAsync(OrderId);
            if (order == null) return NotFound();

            ViewBag.Products = await _orders.GetProductOptionsAsync();
            return View(order);
        }

        // POST: Order/Edit/5 修改訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel EditOrder, int OrderId)
        {
            if (OrderId != EditOrder.OrderId)
                return NotFound();

            var result = await _orders.UpdateOrderAsync(OrderId, EditOrder);
            if (!result.Ok)
            {
                if (result.Code == 404) return NotFound();
                TempData["ErrorMessage"] = result.Message ?? "訂單修改失敗！";
                ViewBag.Products = await _orders.GetProductOptionsAsync();
                return View(nameof(Edit), EditOrder);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Order/ShippedRows - 訂單頁「寄送管理」分頁的表格列（伺服器端以 Razor 輸出，收件人等會員填寫的內容自動編碼）
        [HttpGet]
        public async Task<IActionResult> ShippedRows()
        {
            return PartialView("_ShippedRows", await _shipped.GetShippedListAsync());
        }
    }
}
