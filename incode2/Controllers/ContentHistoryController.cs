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
  [Route("/ContentHistory/{action=Index}/{content_history_id?}")]
  public class ContentHistoryController : Controller
  {
    private readonly incodedatabaseContext _context;

    public ContentHistoryController(incodedatabaseContext context)
    {
      _context = context;
    }

    // GET: ContentHistory
    [HttpGet]
    public async Task<IActionResult> Index()
    {
      var incodedatabaseContext = _context.ContentHistory
        .Include(c => c.Content)
        .Include(c => c.User);
      return View(await incodedatabaseContext.ToListAsync());
    }

    // GET: ContentHistory/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? content_history_id)
    {
      if (content_history_id == null)
      {
        return NotFound();
      }

      var contentHistory = await _context.ContentHistory
          .Include(c => c.Content)
          .Include(c => c.User)
          .FirstOrDefaultAsync(m => m.ContentHistoryId == content_history_id);
      if (contentHistory == null)
      {
        return NotFound();
      }

      return View(contentHistory);
    }

    // GET: ContentHistory/Create
    [HttpGet]
    public IActionResult Create()
    {
      ViewBag.ContentId = new SelectList(_context.Content, "ContentId", "Body");
      ViewBag.UserId = new SelectList(_context.User, "UserId", "Account");
      return View();
    }

    // POST: ContentHistory/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ContentHistoryId,ContentId,UserId,OldTitle,OldBody,Ishidden,EditedAt")] ContentHistory contentHistory)
    {
      if (ModelState.IsValid) //Server端驗證
      {
        _context.Add(contentHistory);
        await _context.SaveChangesAsync(); //寫入資料庫
        return RedirectToAction(nameof(Index)); //重新導向到Index頁面
      }
      ViewBag.ContentId = new SelectList(_context.Content, "ContentId", "Body", contentHistory.ContentId);
      ViewBag.UserId = new SelectList(_context.User, "UserId", "Account", contentHistory.UserId);
      return View(contentHistory);
    }

    // GET: ContentHistory/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? content_history_id)
    {
      if (content_history_id == null)
      {
        return NotFound();
      }

      var contentHistory = await _context.ContentHistory.FindAsync(content_history_id);
      if (contentHistory == null)
      {
        return NotFound();
      }
      ViewBag.ContentId = new SelectList(_context.Content, "ContentId", "Body", contentHistory.ContentId);
      ViewBag.UserId = new SelectList(_context.User, "UserId", "Account", contentHistory.UserId);
      return View(contentHistory);
    }

    // POST: ContentHistory/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int content_history_id, [Bind("ContentHistoryId,ContentId,UserId,OldTitle,OldBody,Ishidden,EditedAt")] ContentHistory contentHistory)
    {
      if (content_history_id != contentHistory.ContentHistoryId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(contentHistory);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ContentHistoryExists(contentHistory.ContentHistoryId))
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
      ViewBag.ContentId = new SelectList(_context.Content, "ContentId", "Body", contentHistory.ContentId);
      ViewBag.UserId = new SelectList(_context.User, "UserId", "Account", contentHistory.UserId);
      return View(contentHistory);
    }

    // GET: ContentHistory/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? content_history_id)
    {
      if (content_history_id == null)
      {
        return NotFound();
      }

      var contentHistory = await _context.ContentHistory
          .Include(c => c.Content)
          .Include(c => c.User)
          .FirstOrDefaultAsync(m => m.ContentHistoryId == content_history_id);
      if (contentHistory == null)
      {
        return NotFound();
      }

      return View(contentHistory);
    }

    // POST: ContentHistory/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int content_history_id)
    {
      var contentHistory = await _context.ContentHistory.FindAsync(content_history_id);
      if (contentHistory != null)
      {
        _context.ContentHistory.Remove(contentHistory);
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool ContentHistoryExists(int content_history_id)
    {
      return _context.ContentHistory.Any(e => e.ContentHistoryId == content_history_id);
    }
  }
}
