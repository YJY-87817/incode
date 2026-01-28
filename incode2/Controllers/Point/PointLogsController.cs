using incode2.Models;
using incode2.Services.Point;
using incode2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace incode2.Controllers.Point
{
    [Route("/PointLogs/{action=Index}/{PointLogId?}")]
    public class PointLogsController : Controller
    {
        private readonly IPointLogService _pointLogService;
        private readonly incodedatabaseContext _context;

        public PointLogsController(IPointLogService pointLogService, incodedatabaseContext context)
        {
            _pointLogService = pointLogService;
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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Username");

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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Username", pointLog.UserId);
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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Username", pointLog.UserId);
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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Username", pointLog.UserId);
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
        public async Task<IActionResult> PointLogsDetail(int? UserId = null)
        {
            // 載入使用者清單給下拉選單
            var userList = await _context.User
                .Select(u => new { u.UserId, u.Username })
                .OrderBy(u => u.UserId)
                .ToListAsync();

            ViewBag.UserList = new SelectList(userList, "UserId", "Username");
            ViewBag.UserCount = userList.Count;

            if (!UserId.HasValue && userList.Any())
                UserId = userList.First().UserId;

            if (!UserId.HasValue)
                return View(new List<PointLogsViewModel>());

            var logs = await _pointLogService.GetDetailedLogsAsync(UserId.Value);

            ViewBag.CurrentTotalPoints = await _pointLogService.GetCurrentBalanceAsync(UserId.Value);
            ViewBag.SelectedUserId = UserId.Value;

            return View(logs);
        }
    }
}
