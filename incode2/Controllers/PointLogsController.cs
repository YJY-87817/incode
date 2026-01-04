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

        // GET: PointLogs/Create
        public IActionResult Create()
        {
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointLog = await _context.PointLog.FindAsync(id);
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
        public async Task<IActionResult> Edit(int id, [Bind("PointLogId,UserId,SourceType,SourceId,PointTypeDefinitionId,ChangeAmount,BalanceAfter,CreatedAt")] PointLog pointLog)
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
            ViewData["PointTypeDefinitionId"] = new SelectList(_context.PointTypeDefinition, "PointTypeDefinitionId", "DisplayName", pointLog.PointTypeDefinitionId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointLog.UserId);
            return View(pointLog);
        }

        // GET: PointLogs/Delete/5
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

        // POST: PointLogs/Delete/5
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
    }
}
