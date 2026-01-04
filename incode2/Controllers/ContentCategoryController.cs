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
  public class ContentCategoryController : Controller
  {
    private readonly incodedatabaseContext _context;

    public ContentCategoryController(incodedatabaseContext context)
    {
      _context = context;
    }

    //// GET: ContentCategory 有改檔案名稱Index_Old.cshtml
    //public async Task<IActionResult> Index()
    //{
    //  var incodedatabaseContext = _context.ContentCategory.Include(c => c.ParentCategory);
    //  return View(await incodedatabaseContext.ToListAsync());
    //}

    // GET: ContentCategory
    public async Task<IActionResult> Index()
    {
      var incodedatabaseContext = _context.ContentCategory.Include(c => c.ParentCategory);
      return View();
    }

    // POST: ContentCategory/IndexJson
    [HttpPost]
    public async Task<IActionResult> IndexJson()
    {
      var category = await _context.ContentCategory
      .Select(c => new
      {
        c.ContentCategoryId,
        c.ParentCategoryId,
        c.CategoryName,
        c.Description,
        //ParentName = c.ParentCategory != null ? c.ParentCategory.CategoryName : "無"
      })
      .ToListAsync();
      return Json(category);
    }

    // GET: ContentCategory/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var contentCategory = await _context.ContentCategory
          .Include(c => c.ParentCategory)
          .FirstOrDefaultAsync(m => m.ContentCategoryId == id);
      if (contentCategory == null)
      {
        return NotFound();
      }

      return View(contentCategory);
    }

    // GET: ContentCategory/Create
    public IActionResult Create()
    {
      ViewData["ParentCategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName");
      return View();
    }

    // POST: ContentCategory/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ContentCategoryId,ParentCategoryId,CategoryName,Description")] ContentCategory contentCategory)
    {
      if (ModelState.IsValid)
      {
        _context.Add(contentCategory);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewData["ParentCategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", contentCategory.ParentCategoryId);
      return View(contentCategory);
    }

    // GET: ContentCategory/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var contentCategory = await _context.ContentCategory.FindAsync(id);
      if (contentCategory == null)
      {
        return NotFound();
      }
      ViewData["ParentCategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", contentCategory.ParentCategoryId);
      return View(contentCategory);
    }

    // POST: ContentCategory/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ContentCategoryId,ParentCategoryId,CategoryName,Description")] ContentCategory contentCategory)
    {
      if (id != contentCategory.ContentCategoryId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(contentCategory);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ContentCategoryExists(contentCategory.ContentCategoryId))
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
      ViewData["ParentCategoryId"] = new SelectList(_context.ContentCategory, "ContentCategoryId", "CategoryName", contentCategory.ParentCategoryId);
      return View(contentCategory);
    }

    // GET: ContentCategory/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var contentCategory = await _context.ContentCategory
          .Include(c => c.ParentCategory)
          .FirstOrDefaultAsync(m => m.ContentCategoryId == id);
      if (contentCategory == null)
      {
        return NotFound();
      }

      return View(contentCategory);
    }

    // POST: ContentCategory/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var contentCategory = await _context.ContentCategory.FindAsync(id);
      if (contentCategory != null)
      {
        _context.ContentCategory.Remove(contentCategory);
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool ContentCategoryExists(int id)
    {
      return _context.ContentCategory.Any(e => e.ContentCategoryId == id);
    }
  }
}
