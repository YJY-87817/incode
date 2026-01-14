using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.Controllers
{
    [Route("/PointLogs/{action=Index}/{PointLogId?}")]
    public class PointLogsController : Controller
    {
        private readonly incodedatabaseContext _context;

        public PointLogsController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: PointLogs
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.PointLog.Include(p => p.PointTypeDefinition).Include(p => p.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: PointLogs/Details/5
        public async Task<IActionResult> Details(int? PointLogId)
        {
            if (PointLogId == null)
            {
                return NotFound();
            }

            var pointLog = await _context.PointLog
                .Include(p => p.PointTypeDefinition)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointLogId == PointLogId);
            if (pointLog == null)
            {
                return NotFound();
            }

            return View(pointLog);
        }

        // GET: PointLogs/Create
        public IActionResult Create()
        {
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");

            var sourceTypes = new List<SelectListItem>
            {
               new SelectListItem { Value = "mission", Text = "平台任務獎勵" },
               new SelectListItem { Value = "entrust_mission", Text = "委託任務獎勵" },
               new SelectListItem { Value = "deposit", Text = "點數儲值" },
               new SelectListItem { Value = "order", Text = "商品購買" },
               //new SelectListItem { Value = "entrust", Text = "委託扣除儲值" }
            };
            ViewBag.SourceTypes = sourceTypes;

            return View();
        }

        // POST: PointLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PointLogId,UserId,SourceType,SourceId,PointTypeDefinitionId,ChangeAmount,BalanceAfter,CreatedAt")] PointLog pointLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pointLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointLog.UserId);
            return View(pointLog);
        }

        // GET: PointLogs/Edit/5
        public async Task<IActionResult> Edit(int? PointLogId)
        {
            if (PointLogId == null)
            {
                return NotFound();
            }

            var pointLog = await _context.PointLog.FindAsync(PointLogId);
            if (pointLog == null)
            {
                return NotFound();
            }
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointLog.UserId);
            return View(pointLog);
        }

        // POST: PointLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int PointLogId, [Bind("PointLogId,UserId,SourceType,SourceId,PointTypeDefinitionId,ChangeAmount,BalanceAfter,CreatedAt")] PointLog pointLog)
        {
            if (PointLogId != pointLog.PointLogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pointLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PointLogExists(pointLog.PointLogId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointLog.UserId);
            return View(pointLog);
        }

        // GET: PointLogs/Delete/5
        public async Task<IActionResult> Delete(int? PointLogId)
        {
            if (PointLogId == null)
            {
                return NotFound();
            }

            var pointLog = await _context.PointLog
                .Include(p => p.PointTypeDefinition)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointLogId == PointLogId);
            if (pointLog == null)
            {
                return NotFound();
            }

            return View(pointLog);
        }

        // POST: PointLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int PointLogId)
        {
            var pointLog = await _context.PointLog.FindAsync(PointLogId);
            if (pointLog != null)
            {
                _context.PointLog.Remove(pointLog);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PointLogExists(int PointLogId)
        {
            return _context.PointLog.Any(e => e.PointLogId == PointLogId);
        }

        //----------------------------------------------------------------------------
        // GET: PointLogs/PointLogsDetail
        public async Task<IActionResult> PointLogsDetail(int? UserId = null)  //目前後台查看任意使用者點數明細 //之後改成登入者 ID
        {

            //載入所有使用者到下拉選單
            var userList = await _context.User
           .Select(u => new { u.UserId, u.Username })
           .OrderBy(u => u.UserId)
           .ToListAsync();

            ViewBag.UserList = new SelectList(userList, "UserId", "Username");
            // ★ 加這行傳總數給 debug 用
            ViewBag.UserCount = userList.Count;

            // 如果沒有選擇使用者，預設顯示第一個
            if (!UserId.HasValue && userList.Any())
            {
                UserId = userList.First().UserId;
            }

            if (!UserId.HasValue || !userList.Any())
            {
                return View(new List<ViewModels.PointLogsViewModel>());
            }

            // 取出該使用者的所有點數異動紀錄（舊 → 新）
            var logs = await _context.PointLog
                .Include(l => l.PointTypeDefinition)
                .Where(l => l.UserId == UserId.Value)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            if (!logs.Any())
            {
                ViewBag.SelectedUserId = UserId.Value;
                ViewBag.CurrentTotalPoints = 0;
                return View(new List<ViewModels.PointLogsViewModel>());
            }

            // 收集各來源類型的 ID
            var missionIds = logs.Where(l => l.SourceType == "mission").Select(l => l.SourceId).Distinct().ToList();

            var entrustMissionIds = logs.Where(l => l.SourceType == "entrust_mission").Select(l => l.SourceId).Distinct().ToList();

            var pointDepositIds = logs.Where(l => l.SourceType == "point_deposits").Select(l => l.SourceId).Distinct().ToList();

            var orderIds = logs.Where(l => l.SourceType == "order").Select(l => l.SourceId).Distinct().ToList();

            //var entrustIds = logs.Where(l => l.SourceType == "user_point_wallet").Select(l => l.SourceId).Distinct().ToList();

            // 批量載入相關資料
            var mission = await _context.Mission
                .Where(m => missionIds.Contains(m.MissionId))
                .ToDictionaryAsync(m => m.MissionId, m => m.MissionName ?? "未知任務");

            var entrustMission = await _context.EntrustMission
                .Where(em => entrustMissionIds.Contains(em.EntrustMissionId))
                .ToDictionaryAsync(em => em.EntrustMissionId, em => em.Title ?? "未知委託");

            var pointDepost = await _context.PointRecharge
                .Where(pd => pointDepositIds.Contains(pd.PointRechargeId))
                .ToDictionaryAsync(pd => pd.PointRechargeId, pd => pd.PaymentMethod ?? "未知付款方式");

            var orderDict = new Dictionary<int, Orders>();
            if (orderIds.Any())
            {
                orderDict = await _context.Orders
                    .Include(o => o.Orderdetail)
                        .ThenInclude(od => od.Product)
                    .Where(o => orderIds.Contains(o.OrderId))
                    .AsNoTracking()           // ← 加這行，唯讀查詢更快
                    .ToDictionaryAsync(o => o.OrderId, o => o);
            }

            // 建立 ViewModel
            var viewModels = new List<ViewModels.PointLogsViewModel>();
            int runningBalance = 0;

            foreach (var log in logs)
            {
                runningBalance += log.ChangeAmount;

                string description = log.SourceType switch
                {
                    "mission" => mission.GetValueOrDefault(log.SourceId, "未知任務"),

                    "entrust_mission" => entrustMission.GetValueOrDefault(log.SourceId, "未知委託"),

                    "order" => orderDict.TryGetValue(log.SourceId, out var o)
                          ? GetOrderDisplayText(o)
                          : $"訂單 #{log.SourceId}（找不到資料）",

                    "deposit" => $"點數儲值紀錄 #{log.SourceId}",

                    _ => string.IsNullOrEmpty(log.SourceType) ? "系統調整" : $"其他：{log.SourceType}"
                };

                viewModels.Add(new ViewModels.PointLogsViewModel
                {
                    CreatedAt = log.CreatedAt ?? DateTime.Now,
                    ChangeAmount = log.ChangeAmount,
                    Description = description,
                    NewBalance = runningBalance,
                    TypeDisplayName = log.PointTypeDefinition?.DisplayName ?? "未知類型"
                });
            }

            // 最新紀錄顯示在上
            viewModels.Reverse();

            ViewBag.CurrentTotalPoints = runningBalance;
            ViewBag.SelectedUserId = UserId.Value;

            return View(viewModels);
        }

        // 輔助方法：美化點數商店訂單顯示文字
        private string GetOrderDisplayText(Orders order)
        {
            if (order?.Orderdetail == null || !order.Orderdetail.Any())
                return $"點數商店消費（訂單 #{order?.OrderId})";

            var details = order.Orderdetail;
            var firstProductName = details.First().Product?.Name ?? "商品";

            if (details.Count == 1)
            {
                var qty = details.First().Quantity;
                return qty == 1
                    ? $"購買 {firstProductName}（訂單 #{order.OrderId}）"
                    : $"購買 {firstProductName} × {qty}（訂單 #{order.OrderId}）";
            }

            // 多項商品
            var totalItems = details.Sum(d => d.Quantity);
            return $"購買 {firstProductName} 等 {totalItems} 件（訂單 #{order.OrderId}）";
        }
    
    }
}
