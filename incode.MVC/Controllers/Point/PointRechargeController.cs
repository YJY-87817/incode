using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class PointRechargeController : Controller
    {
        private readonly IAdminPointQueryService _query;
        private readonly IPointLogService _pointLogService;
        private readonly ILogger<PointRechargeController> _logger;

        public PointRechargeController(IAdminPointQueryService query, IPointLogService pointLogService, ILogger<PointRechargeController> logger)
        {
            _query = query;
            _pointLogService = pointLogService;
            _logger = logger;
        }

        // GET: PointRecharge
        public async Task<IActionResult> Index()
        {
            return View(await _query.GetRechargesAsync());
        }

        // GET: PointRecharge/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var pointRecharge = await _query.GetRechargeAsync(id.Value);
            if (pointRecharge == null) return NotFound();
            return View(pointRecharge);
        }

        // 儲值單不能直接新增 / 修改 / 刪除：原本把狀態改成 success 不會加點，刪除也會讓點數紀錄失去來源。
        // 會員付款了但綠界通知沒進來（或被標成失敗）時，用「人工補單」（與綠界回呼共用 CompleteRechargeAsync，不會重複加點）

        // POST: PointRecharge/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? paymentTradeNo)
        {
            try
            {
                await _pointLogService.AdminCompleteRechargeAsync(id, paymentTradeNo);
                _logger.LogInformation("管理員 {AdminUserId} 人工補單儲值單 {PointRechargeId}（{PaymentTradeNo}）",
                    User.GetId(), id, paymentTradeNo);
                TempData["SuccessMessage"] = $"儲值單 #{id} 已補單並加點";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
