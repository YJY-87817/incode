using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class PointLogController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;
        private readonly ILogger<PointLogController> _logger;

        public PointLogController(incodedatabaseContext context, IPointLogService pointLogService, ILogger<PointLogController> logger)
        {
            _context = context;
            _pointLogService = pointLogService;
            _logger = logger;
        }

        // GET: PointLogTest
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.PointLog.Include(p => p.PointTypeDefinition).Include(p => p.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: PointLogTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointLog = await _context.PointLog
                .Include(p => p.PointTypeDefinition)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointLogId == id);
            if (pointLog == null)
            {
                return NotFound();
            }

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
            var users = await _context.User
                .AsNoTracking()
                .OrderBy(u => u.UserId)
                .Select(u => new { u.UserId, Text = "#" + u.UserId + " " + u.Username + "（" + u.Email + "）" })
                .ToListAsync();
            ViewBag.UserList = new SelectList(users, "UserId", "Text", selectedUserId);
        }

        //-----------------------------------------------------------
        // GET: PointLogTest/PointLogsDetail
        public async Task<IActionResult> PointLogsDetail(int? UserId = null)
        {
            // 1. 載入使用者清單給下拉選單
            var userList = await _context.User
                .Select(u => new { u.UserId, u.Username, u.Email })
                .OrderBy(u => u.UserId)
                .ToListAsync();

            ViewBag.UserList = new SelectList(userList, "UserId", "Username");
            ViewBag.UserCount = userList.Count;

            // 如果沒傳 UserId，預設選第一個
            if (!UserId.HasValue && userList.Any())
                UserId = userList.First().UserId;

            if (!UserId.HasValue)
                return View(new List<PointLogsViewModel>());

            // 2. 抓取該使用者的所有 Log
            var logs = await _context.PointLog
                .AsNoTracking()
                .Include(l => l.PointTypeDefinition)
                .Where(l => l.UserId == UserId.Value)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // 3. 準備批量查詢相關資料 (避免在迴圈內反覆下 SQL)
            var missionIds = logs.Where(l => l.SourceType == "mission").Select(l => l.SourceId).Distinct().ToList();
            var entrustIds = logs.Where(l => l.SourceType == "entrust_mission" || l.SourceType == "entrust_mission_point_reserve")
                                 .Select(l => l.SourceId).Distinct().ToList();
            var rechargeIds = logs.Where(l => l.SourceType == "point_recharge").Select(l => l.SourceId).Distinct().ToList();

            var missions = await _context.Mission.Where(m => missionIds.Contains(m.MissionId))
                .ToDictionaryAsync(m => m.MissionId, m => m.MissionName ?? "任務");

            var entrusts = await _context.EntrustMission.Where(e => entrustIds.Contains(e.EntrustMissionId))
                .ToDictionaryAsync(e => e.EntrustMissionId, e => e.Title ?? "委託");

            // 4. 轉換為 ViewModel
            var viewModel = logs.Select(log =>
            {
                string specificTitle = log.SourceType switch
                {
                    "mission" => missions.GetValueOrDefault(log.SourceId ?? 0, "平台任務獎勵"),
                    "entrust_mission" => entrusts.GetValueOrDefault(log.SourceId ?? 0, "委託任務獎勵"),
                    "entrust_mission_point_reserve" => entrusts.TryGetValue(log.SourceId ?? 0, out var t) ? $"[預扣] {t}" : "委託預扣",
                    "point_recharge" => "點數儲值",
                    "orders" => $"訂單編號 #{log.SourceId}", // 這裡直接顯示 ID 或是你可以補 Order 查詢
                    _ => log.DisplaySource ?? "系統調整"
                };

                return new PointLogsViewModel
                {
                    CreatedAt = log.CreatedAt ?? DateTime.Now,
                    ChangeAmount = log.ChangeAmount,
                    NewBalance = log.BalanceAfter,
                    DisplayName = log.PointTypeDefinition?.Category ?? "點數異動",
                    Description = specificTitle
                };
            }).ToList();

            // 5. 計算當前餘額 (ViewBag)
            ViewBag.CurrentTotalPoints = logs.OrderByDescending(l => l.CreatedAt)
                                            .ThenByDescending(l => l.PointLogId)
                                            .Select(l => l.BalanceAfter)
                                            .FirstOrDefault();

            ViewBag.SelectedUserId = UserId.Value;

            return View(viewModel);
        }
    }
}
