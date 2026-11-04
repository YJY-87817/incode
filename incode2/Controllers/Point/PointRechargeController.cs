using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class PointRechargeController : Controller
    {
        private readonly incodedatabaseContext _context;

        public PointRechargeController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: PointRecharge
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.PointRecharge.Include(p => p.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: PointRecharge/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointRecharge = await _context.PointRecharge
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointRechargeId == id);
            if (pointRecharge == null)
            {
                return NotFound();
            }

            return View(pointRecharge);
        }

        // GET: PointRecharge/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email");
            return View();
        }

        // POST: PointRecharge/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PointRechargeId,UserId,OrderNo,CashAmount,PointsGranted,PaymentMethod,PaymentTradeNo,Status,CreatedAt")] PointRecharge pointRecharge)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pointRecharge);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointRecharge.UserId);
            return View(pointRecharge);
        }

        // GET: PointRecharge/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointRecharge = await _context.PointRecharge
                .Include(p => p.User) // <--- 確保這行存在
                .FirstOrDefaultAsync(m => m.PointRechargeId == id);

            if (pointRecharge == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointRecharge.UserId);
            return View(pointRecharge);
        }

        // POST: PointRecharge/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PointRechargeId,UserId,OrderNo,CashAmount,PointsGranted,PaymentMethod,PaymentTradeNo,Status,CreatedAt")] PointRecharge pointRecharge)
        {
            if (id != pointRecharge.PointRechargeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pointRecharge);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PointRechargeExists(pointRecharge.PointRechargeId))
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
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", pointRecharge.UserId);
            return View(pointRecharge);
        }

        // GET: PointRecharge/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointRecharge = await _context.PointRecharge
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PointRechargeId == id);
            if (pointRecharge == null)
            {
                return NotFound();
            }

            return View(pointRecharge);
        }

        // POST: PointRecharge/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pointRecharge = await _context.PointRecharge.FindAsync(id);
            if (pointRecharge != null)
            {
                _context.PointRecharge.Remove(pointRecharge);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PointRechargeExists(int id)
        {
            return _context.PointRecharge.Any(e => e.PointRechargeId == id);
        }
    }
}
