using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.Controllers.Point
{
    public class MissionProgressController : Controller
    {
        private readonly incodedatabaseContext _context;

        public MissionProgressController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: MissionProgressTest
        public async Task<IActionResult> Index(int? MissionId, int? UserId)
        {
            var query = _context.MissionProgress
                 .Include(m => m.Mission)
                 .Include(m => m.User)
                 .AsNoTracking();

            if (MissionId.HasValue)
            {
                query = query.Where(m => m.MissionId == MissionId.Value);
            }
            else if (UserId.HasValue) // 調整邏輯：如果查用戶，就不查特定任務
            {
                query = query.Where(m => m.UserId == UserId.Value);
            }

            query = query.OrderBy(m => m.MissionId)
                         .ThenBy(m => m.UserId);

            var progress = await query.ToListAsync();

            // --- 優化後的標題設定邏輯 ---
            if (MissionId.HasValue)
            {
                // 直接從查詢結果的第一筆資料獲取任務名稱，無需再次查詢資料庫
                var firstItem = progress.FirstOrDefault();
                ViewBag.PageTitle = $"任務進度 - {firstItem?.Mission?.MissionName ?? "未找到任務"}";
            }
            else if (UserId.HasValue)
            {
                var firstItem = progress.FirstOrDefault();
                ViewBag.PageTitle = $"用戶任務進度 - {firstItem?.User?.Email ?? "未找到用戶"}";
            }
            else
            {
                ViewBag.PageTitle = "所有任務進度記錄";
            }

            return View(progress);
        }

        // GET: MissionProgressTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var missionProgress = await _context.MissionProgress
                .Include(m => m.Mission)
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MissionProgressId == id);
            if (missionProgress == null)
            {
                return NotFound();
            }

            return View(missionProgress);
        }

        // GET: MissionProgressTest/Create
        public IActionResult Create()
        {
            ViewData["MissionId"] = new SelectList(_context.Mission, "MissionId", "Category");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email");
            return View();
        }

        // POST: MissionProgressTest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MissionProgressId,UserId,MissionId,CurrentCount,Status,AcceptedAt,CompletedAt,RewardedAt,CycleStartAt,CycleEndAt")] MissionProgress missionProgress)
        {
            if (ModelState.IsValid)
            {
                _context.Add(missionProgress);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MissionId"] = new SelectList(_context.Mission, "MissionId", "Category", missionProgress.MissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", missionProgress.UserId);
            return View(missionProgress);
        }

        // GET: MissionProgressTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var missionProgress = await _context.MissionProgress
                .Include(m => m.User)    // 確保有 Include 關聯物件
                .Include(m => m.Mission) // 確保有 Include 關聯物件
                .FirstOrDefaultAsync(m => m.MissionProgressId == id);

            if (missionProgress == null)
            {
                return NotFound();
            }


            // 2. 將關聯資料的名稱放入 ViewBag
            ViewBag.UserEmail = missionProgress.User?.Email ?? "未知會員";
            ViewBag.MissionName = missionProgress.Mission?.MissionName ?? "未知任務";

            // 原有的下拉選單資料 (如果還有需要的話)
            ViewData["MissionId"] = new SelectList(_context.Mission, "MissionId", "MissionName", missionProgress.MissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", missionProgress.UserId);

            return View(missionProgress);
        }

        // POST: MissionProgressTest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MissionProgressId,UserId,MissionId,CurrentCount,Status,AcceptedAt,CompletedAt,RewardedAt,CycleStartAt,CycleEndAt")] MissionProgress missionProgress)
        {
            if (id != missionProgress.MissionProgressId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(missionProgress);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MissionProgressExists(missionProgress.MissionProgressId))
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
            ViewData["MissionId"] = new SelectList(_context.Mission, "MissionId", "Category", missionProgress.MissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", missionProgress.UserId);
            return View(missionProgress);
        }

        // GET: MissionProgressTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var missionProgress = await _context.MissionProgress
                .Include(m => m.Mission)
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MissionProgressId == id);
            if (missionProgress == null)
            {
                return NotFound();
            }

            return View(missionProgress);
        }

        // POST: MissionProgressTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var missionProgress = await _context.MissionProgress.FindAsync(id);
            if (missionProgress != null)
            {
                _context.MissionProgress.Remove(missionProgress);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MissionProgressExists(int id)
        {
            return _context.MissionProgress.Any(e => e.MissionProgressId == id);
        }
    }
}
