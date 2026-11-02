using incode2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ShippedController : Controller
    {
        private readonly incodedatabaseContext _context;
        public ShippedController(incodedatabaseContext context)
        {
            _context = context;
        }
        // GET: Shipped
        [HttpGet]
        public ActionResult Index()
        {

            return View(_context.ShippedDetail.Include(e => e.Order).ToList());
        }

        // GET: Shipped/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Shipped/Create 存入資料庫
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShippedDetail ShippedDetail)
        {
            ShippedDetail.IsFailed = false;
            ShippedDetail.IsCompleted = false;
            _context.Add(ShippedDetail);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Order");
        }

        // GET: Shipped/Edit/5
        [Route("/Shipped/Edit/{ShippedDetailId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int ShippedDetailId)
        {
            var ShippedDetail = await _context.ShippedDetail.FindAsync(ShippedDetailId);
            if (ShippedDetail == null)
            {
                return NotFound();
            }
            return View(ShippedDetail);
        }

        // POST: Shipped/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ShippedDetailId, ShippedDetail ShippedDetail)
        {
            if (ShippedDetailId != ShippedDetail.ShippedDetailId) return NotFound();
            _context.Update(ShippedDetail);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Order");
        }

        // GET: Products/Delete/5
        [Route("/Shipped/Delete/{ShippedDetailId}")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? ShippedDetailId)
        {
            if (ShippedDetailId == null)
            {
                return NotFound();
            }

            var ShippedDetail = await _context.ShippedDetail
                .FirstOrDefaultAsync(m => m.ShippedDetailId == ShippedDetailId);
            if (ShippedDetail == null)
            {
                return NotFound();
            }

            return View(ShippedDetail);
        }

        // POST: Shipped/Delete/5
        [Route("/Shipped/Delete/{ShippedDetailId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int ShippedDetailId)
        {
            var ShippedDetail = await _context.ShippedDetail.FindAsync(ShippedDetailId);
            if (ShippedDetail != null)
            {
                _context.ShippedDetail.Remove(ShippedDetail);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Order");
        }
    }
}
