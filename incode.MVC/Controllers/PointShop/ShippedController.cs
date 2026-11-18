using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ShippedController : Controller
    {
        private readonly IAdminShippedService _shipped;

        public ShippedController(IAdminShippedService shipped)
        {
            _shipped = shipped;
        }

        // GET: Shipped
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await _shipped.GetShippedListAsync());
        }

        // GET: Shipped/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Shipped/Create 存入資料庫
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,RecipientName,RecipientAddress,RecipientPhone,ShippedDate,ArrivalDate,Remark")] ShippedDetail ShippedDetail)
        {
            try
            {
                await _shipped.CreateShippedAsync(ShippedDetail);
                TempData["SuccessMessage"] = "寄送資料已新增";
                return RedirectToAction("Index", "Order");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(ShippedDetail);
            }
        }

        // GET: Shipped/Edit/5
        [Route("/Shipped/Edit/{ShippedDetailId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int ShippedDetailId)
        {
            var shipped = await _shipped.GetShippedAsync(ShippedDetailId);
            if (shipped == null) return NotFound();
            return View(shipped);
        }

        // POST: Shipped/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ShippedDetailId,
            [Bind("ShippedDetailId,OrderId,RecipientName,RecipientAddress,RecipientPhone,ShippedDate,ArrivalDate,Remark,IsFailed,IsCompleted")] ShippedDetail ShippedDetail)
        {
            if (ShippedDetailId != ShippedDetail.ShippedDetailId) return NotFound();

            try
            {
                if (!await _shipped.UpdateShippedAsync(ShippedDetailId, ShippedDetail)) return NotFound();
                TempData["SuccessMessage"] = "寄送資料已更新";
                return RedirectToAction("Index", "Order");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(ShippedDetail);
            }
        }

        // GET: Shipped/Delete/5
        [Route("/Shipped/Delete/{ShippedDetailId}")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? ShippedDetailId)
        {
            if (ShippedDetailId == null) return NotFound();
            var shipped = await _shipped.GetShippedAsync(ShippedDetailId.Value);
            if (shipped == null) return NotFound();
            return View(shipped);
        }

        // POST: Shipped/Delete/5
        [Route("/Shipped/Delete/{ShippedDetailId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int ShippedDetailId)
        {
            try
            {
                if (await _shipped.DeleteShippedAsync(ShippedDetailId))
                    TempData["SuccessMessage"] = "寄送資料已刪除";
                else
                    TempData["ErrorMessage"] = "找不到這筆寄送資料（可能已被刪除）";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Index", "Order");
        }
    }
}
