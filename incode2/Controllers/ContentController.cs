using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using incode2.Models;
using incode2.Models.ViewModels;

namespace incode2.Controllers
{
  public class ContentController : Controller
  {
    private readonly incodedatabaseContext _context;

    // 建構函式注入
    public ContentController(incodedatabaseContext context)
    {
      _context = context;
    }

    // GET: Content
    public IActionResult Index()
    {
      // 查詢所有貼文（包含作者和分類）
      var contentList = _context.Content
          .Include(c => c.User)              // 關聯查詢：作者
          .Include(c => c.Category)          // 關聯查詢：分類
          .Where(c => c.ParentContentId == null)  // 只要主貼文（不要回覆）
          .OrderByDescending(c => c.CreatedAt)    // 最新的在前面
          .Select(c => new ContentListViewModel
          {
            ContentId = c.ContentId,
            Title = c.Title,
            Body = c.Body.Substring(0, Math.Min(100, c.Body.Length)), // 只取前100字
            CreatedAt = (DateTime)c.CreatedAt,
            AuthorName = c.User.Username,
            CategoryName = c.Category.CategoryName
          })
          .ToList();

      return View(contentList);
    }
  }
}