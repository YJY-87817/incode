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
    public class EntrustMissionProgressController : Controller
    {
        private readonly incodedatabaseContext _context;

        public EntrustMissionProgressController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: EntrustMissionProgress
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.EntrustMissionProgress.Include(e => e.EntrustMission).Include(e => e.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: EntrustMissionProgress/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMissionProgress = await _context.EntrustMissionProgress
                .Include(e => e.EntrustMission)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionProgressId == id);
            if (entrustMissionProgress == null)
            {
                return NotFound();
            }

            return View(entrustMissionProgress);
        }

        // GET: EntrustMissionProgress/Create
        public IActionResult Create()
        {
            ViewData["EntrustMissionId"] = new SelectList(_context.EntrustMission, "EntrustMissionId", "Detail");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");
            return View();
        }

        // POST: EntrustMissionProgress/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntrustMissionProgressId,UserId,EntrustMissionId,Status,AcceptedAt,CompletedAt,RewardedAt")] EntrustMissionProgress entrustMissionProgress)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entrustMissionProgress);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EntrustMissionId"] = new SelectList(_context.EntrustMission, "EntrustMissionId", "Detail", entrustMissionProgress.EntrustMissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", entrustMissionProgress.UserId);
            return View(entrustMissionProgress);
        }

        // GET: EntrustMissionProgress/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMissionProgress = await _context.EntrustMissionProgress.FindAsync(id);
            if (entrustMissionProgress == null)
            {
                return NotFound();
            }
            ViewData["EntrustMissionId"] = new SelectList(_context.EntrustMission, "EntrustMissionId", "Detail", entrustMissionProgress.EntrustMissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", entrustMissionProgress.UserId);
            return View(entrustMissionProgress);
        }

        // POST: EntrustMissionProgress/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EntrustMissionProgressId,UserId,EntrustMissionId,Status,AcceptedAt,CompletedAt,RewardedAt")] EntrustMissionProgress entrustMissionProgress)
        {
            if (id != entrustMissionProgress.EntrustMissionProgressId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(entrustMissionProgress);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntrustMissionProgressExists(entrustMissionProgress.EntrustMissionProgressId))
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
            ViewData["EntrustMissionId"] = new SelectList(_context.EntrustMission, "EntrustMissionId", "Detail", entrustMissionProgress.EntrustMissionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", entrustMissionProgress.UserId);
            return View(entrustMissionProgress);
        }

        // GET: EntrustMissionProgress/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMissionProgress = await _context.EntrustMissionProgress
                .Include(e => e.EntrustMission)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionProgressId == id);
            if (entrustMissionProgress == null)
            {
                return NotFound();
            }

            return View(entrustMissionProgress);
        }

        // POST: EntrustMissionProgress/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrustMissionProgress = await _context.EntrustMissionProgress.FindAsync(id);
            if (entrustMissionProgress != null)
            {
                _context.EntrustMissionProgress.Remove(entrustMissionProgress);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EntrustMissionProgressExists(int id)
        {
            return _context.EntrustMissionProgress.Any(e => e.EntrustMissionProgressId == id);
        }
    }
}
