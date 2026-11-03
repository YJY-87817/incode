using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Controllers.Point
{
    public class PointLogController : Controller
    {
        private readonly incodedatabaseContext _context;

        public PointLogController(incodedatabaseContext context)
        {
            _context = context;
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

        // GET: PointLogTest/Create
        public IActionResult Create()
        {
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "Category");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email");

            var sourceTypes = new List<SelectListItem>
            {
               new SelectListItem { Value = "mission", Text = "平台任務獎勵" },
               new SelectListItem { Value = "entrust_mission", Text = "委託任務獎勵" },
               new SelectListItem { Value = "entrust_mission_point_reserve", Text = "委託預扣點數" },
               new SelectListItem { Value = "point_recharge", Text = "點數儲值" },
               new SelectListItem { Value = "order", Text = "商品購買" },
               new SelectListItem { Value = "system", Text = "系統調整" }
            };
            ViewBag.SourceTypes = sourceTypes;

            return View();
        }

        // POST: PointLogTest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PointLogId,UserId,SourceType,SourceId,DisplaySource,DisplaySourceSubTitle,PointTypeDefinitionId,ChangeAmount,BalanceAfter,Status,CreatedAt")] PointLog pointLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pointLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "Category", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointLog.UserId);
            return View(pointLog);
        }

        // GET: PointLogTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "Category", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointLog.UserId);
            return View(pointLog);
        }

        // POST: PointLogTest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PointLogId,UserId,SourceType,SourceId,DisplaySource,DisplaySourceSubTitle,PointTypeDefinitionId,ChangeAmount,BalanceAfter,Status,CreatedAt")] PointLog pointLog)
        {
            if (id != pointLog.PointLogId)
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
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "Category", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointLog.UserId);
            return View(pointLog);
        }

        // GET: PointLogTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: PointLogTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pointLog = await _context.PointLog.FindAsync(id);
            if (pointLog != null)
            {
                _context.PointLog.Remove(pointLog);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PointLogExists(int id)
        {
            return _context.PointLog.Any(e => e.PointLogId == id);
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
