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
    public class PointDepositsController : Controller
    {
        private readonly incodedatabaseContext _context;

        public PointDepositsController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: PointDeposits
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.PointDeposits.Include(p => p.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: PointDeposits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointDeposits = await _context.PointDeposits
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointDepositId == id);
            if (pointDeposits == null)
            {
                return NotFound();
            }

            return View(pointDeposits);
        }

        // GET: PointDeposits/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");
            return View();
        }

        // POST: PointDeposits/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PointDepositId,UserId,OrderNo,CashAmount,PointsGranted,PaymentMethod,RelatedOrderId,Status,CreatedAt")] PointDeposits pointDeposits)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pointDeposits);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointDeposits.UserId);
            return View(pointDeposits);
        }

        // GET: PointDeposits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointDeposits = await _context.PointDeposits.FindAsync(id);
            if (pointDeposits == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointDeposits.UserId);
            return View(pointDeposits);
        }

        // POST: PointDeposits/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PointDepositId,UserId,OrderNo,CashAmount,PointsGranted,PaymentMethod,RelatedOrderId,Status,CreatedAt")] PointDeposits pointDeposits)
        {
            if (id != pointDeposits.PointDepositId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pointDeposits);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PointDepositsExists(pointDeposits.PointDepositId))
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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", pointDeposits.UserId);
            return View(pointDeposits);
        }

        // GET: PointDeposits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointDeposits = await _context.PointDeposits
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointDepositId == id);
            if (pointDeposits == null)
            {
                return NotFound();
            }

            return View(pointDeposits);
        }

        // POST: PointDeposits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pointDeposits = await _context.PointDeposits.FindAsync(id);
            if (pointDeposits != null)
            {
                _context.PointDeposits.Remove(pointDeposits);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PointDepositsExists(int id)
        {
            return _context.PointDeposits.Any(e => e.PointDepositId == id);
        }
    }
}
