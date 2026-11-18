using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IAdminShopService _shop;

        public OrderController(IAdminShopService shop)
        {
            _shop = shop;
        }

        // GET: Order
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await _shop.GetOrdersAsync());
        }

        // GET: Order/Create 顯示頁面
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _shop.GetProductOptionsAsync();
            return View();
        }

        // POST: Order/Create 新增訂單
        // 總點數、單價、是否需寄送一律由後端依商品資料計算，不採用表單傳入的值
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel NewOrder)
        {
            var result = await _shop.CreateOrderAsync(NewOrder);
            if (!result.Ok)
                return await CreateFailedAsync(NewOrder, result.Message);

            TempData["SuccessMessage"] = "訂單建立成功！";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> CreateFailedAsync(OrderViewModel model, string? message)
        {
            TempData["ErrorMessage"] = message ?? "訂單建立失敗！";
            ViewBag.Products = await _shop.GetProductOptionsAsync();
            return View(nameof(Create), model);
        }

        // GET: Order/Edit/5 顯示頁面
        [Route("/Order/Edit/{OrderId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int OrderId)
        {
            var order = await _shop.GetOrderForEditAsync(OrderId);
            if (order == null) return NotFound();

            ViewBag.Products = await _shop.GetProductOptionsAsync();
            return View(order);
        }

        // POST: Order/Edit/5 修改訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderViewModel EditOrder, int OrderId)
        {
            if (OrderId != EditOrder.OrderId)
                return NotFound();

            var result = await _shop.UpdateOrderAsync(OrderId, EditOrder);
            if (!result.Ok)
            {
                if (result.Code == 404) return NotFound();
                TempData["ErrorMessage"] = result.Message ?? "訂單修改失敗！";
                ViewBag.Products = await _shop.GetProductOptionsAsync();
                return View(nameof(Edit), EditOrder);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Order/GetShippedData - 提供寄送資料的 API
        [HttpGet]
        public async Task<JsonResult> GetShippedData()
        {
            return Json(await _shop.GetShippedRowsAsync());
        }
    }
}
