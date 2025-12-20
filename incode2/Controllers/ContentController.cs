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
    public class ContentController : Controller
    {
        private readonly incodedatabaseContext _context;

        public ContentController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: Content
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.Content.Include(c => c.Category).Include(c => c.ParentContent).Include(c => c.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: Content/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var content = await _context.Content
                .Include(c => c.Category)
                .Include(c => c.ParentContent)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ContentId == id);
            if (content == null)
            {
                return NotFound();
            }

            return View(content);
        }

        // GET: Content/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName");
            ViewData["ParentContentId"] = new SelectList(_context.Content, "ContentId", "Body");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");
            return View();
        }

        // POST: Content/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContentId,ParentContentId,CategoryId,UserId,Title,Body,CreatedAt,UpdatedAt,Ishidden")] Content content)
        {
            if (ModelState.IsValid)
            {
                _context.Add(content);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", content.CategoryId);
            ViewData["ParentContentId"] = new SelectList(_context.Content, "ContentId", "Body", content.ParentContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", content.UserId);
            return View(content);
        }

        // GET: Content/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var content = await _context.Content.FindAsync(id);
            if (content == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", content.CategoryId);
            ViewData["ParentContentId"] = new SelectList(_context.Content, "ContentId", "Body", content.ParentContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", content.UserId);
            return View(content);
        }

        // POST: Content/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContentId,ParentContentId,CategoryId,UserId,Title,Body,CreatedAt,UpdatedAt,Ishidden")] Content content)
        {
            if (id != content.ContentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(content);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContentExists(content.ContentId))
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
            ViewData["CategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", content.CategoryId);
            ViewData["ParentContentId"] = new SelectList(_context.Content, "ContentId", "Body", content.ParentContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", content.UserId);
            return View(content);
        }

        // GET: Content/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var content = await _context.Content
                .Include(c => c.Category)
                .Include(c => c.ParentContent)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ContentId == id);
            if (content == null)
            {
                return NotFound();
            }

            return View(content);
        }

        // POST: Content/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.Content.FindAsync(id);
            if (content != null)
            {
                _context.Content.Remove(content);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContentExists(int id)
        {
            return _context.Content.Any(e => e.ContentId == id);
        }
    }
}
