using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class PointLogController : Controller
    {
        private readonly IAdminPointQueryService _query;
        private readonly IPointLogService _pointLogService;
        private readonly ILogger<PointLogController> _logger;

        public PointLogController(IAdminPointQueryService query, IPointLogService pointLogService, ILogger<PointLogController> logger)
        {
            _query = query;
            _pointLogService = pointLogService;
            _logger = logger;
        }

        // GET: PointLog
        public async Task<IActionResult> Index()
        {
            return View(await _query.GetLogsAsync());
        }

        // GET: PointLog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var pointLog = await _query.GetLogAsync(id.Value);
            if (pointLog == null) return NotFound();
            return View(pointLog);
        }

        // 點數紀錄是帳本（每筆的 BalanceAfter 接續上一筆），不能直接新增 / 修改 / 刪除：
        // 原本的 Create / Edit / Delete 可以手填餘額，會讓帳本斷掉。調整點數一律用下方的 Adjust（走 ChangePointsAsync）

        // GET: PointLog/Adjust
        public async Task<IActionResult> Adjust(int? userId)
        {
            await LoadUserListAsync(userId);
            return View(new PointAdjustViewModel { UserId = userId ?? 0 });
        }

        // POST: PointLog/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(PointAdjustViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _pointLogService.AdminAdjustPointsAsync(vm.UserId, vm.ChangeAmount, vm.Reason);
                    _logger.LogInformation("管理員 {AdminUserId} 調整會員 {UserId} 點數 {ChangeAmount}：{Reason}",
                        User.GetId(), vm.UserId, vm.ChangeAmount, vm.Reason);
                    TempData["SuccessMessage"] = $"已調整點數 {vm.ChangeAmount:+#,0;-#,0}";
                    return RedirectToAction(nameof(PointLogsDetail), new { UserId = vm.UserId });
                }
                catch (BusinessException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            await LoadUserListAsync(vm.UserId);
            return View(vm);
        }

        private async Task LoadUserListAsync(int? selectedUserId)
        {
            var users = (await _query.GetUserOptionsAsync())
                .Select(u => new { u.UserId, Text = $"#{u.UserId} {u.Username}（{u.Email}）" });
            ViewBag.UserList = new SelectList(users, "UserId", "Text", selectedUserId);
        }

        // GET: PointLog/PointLogsDetail
        public async Task<IActionResult> PointLogsDetail(int? UserId = null)
        {
            var users = await _query.GetUserOptionsAsync();
            ViewBag.UserList = new SelectList(users.Select(u => new { u.UserId, u.Username }), "UserId", "Username");
            ViewBag.UserCount = users.Count;

            // 沒有指定會員：預設第一位
            UserId ??= users.Count > 0 ? users[0].UserId : null;
            if (!UserId.HasValue)
                return View(new List<PointLogsViewModel>());

            var (logs, balance) = await _query.GetUserLedgerAsync(UserId.Value);
            ViewBag.CurrentTotalPoints = balance;
            ViewBag.SelectedUserId = UserId.Value;
            return View(logs);
        }
    }
}
