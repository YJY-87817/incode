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
    public class EntrustMissionController : Controller
    {
        private readonly incodedatabaseContext _context;

        public EntrustMissionController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: EntrustMissionTest
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.EntrustMission.Include(e => e.BestContent).Include(e => e.BestUser).Include(e => e.Content).Include(e => e.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: EntrustMissionTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }

            return View(entrustMission);
        }

        // GET: EntrustMissionTest/Create
        public IActionResult Create()
        {
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body");
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email");
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email");
            return View();
        }

        // POST: EntrustMissionTest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntrustMissionId,UserId,ContentId,Title,Detail,RewardPoints,Category,Deadline,Status,IsUrgent,IsActive,CreatedAt,UpdatedAt,BestContentId,BestUserId,SelectedAt")] EntrustMission entrustMission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entrustMission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.BestContentId);
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.BestUserId);
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.ContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.UserId);
            return View(entrustMission);
        }

        // GET: EntrustMissionTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission.FindAsync(id);
            if (entrustMission == null)
            {
                return NotFound();
            }
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.BestContentId);
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.BestUserId);
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.ContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.UserId);
            return View(entrustMission);
        }

        // POST: EntrustMissionTest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EntrustMissionId,UserId,ContentId,Title,Detail,RewardPoints,Category,Deadline,Status,IsUrgent,IsActive,CreatedAt,UpdatedAt,BestContentId,BestUserId,SelectedAt")] EntrustMission entrustMission)
        {
            if (id != entrustMission.EntrustMissionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(entrustMission);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntrustMissionExists(entrustMission.EntrustMissionId))
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
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.BestContentId);
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.BestUserId);
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.ContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.UserId);
            return View(entrustMission);
        }

        // GET: EntrustMissionTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }

            return View(entrustMission);
        }

        // POST: EntrustMissionTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrustMission = await _context.EntrustMission.FindAsync(id);
            if (entrustMission != null)
            {
                _context.EntrustMission.Remove(entrustMission);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EntrustMissionExists(int id)
        {
            return _context.EntrustMission.Any(e => e.EntrustMissionId == id);
        }
    }
}
