using incode2.Models;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers.Contents
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]

    public class ContentController : Controller
  {
    private readonly incodedatabaseContext _context;

    // 建構函式注入
    public ContentController(incodedatabaseContext context)
    {
      _context = context;
    }

    // GET: Content
    [HttpGet]
    public IActionResult Index()
    {
      // 查詢所有貼文（包含作者和分類）
      var contentList = _context.Content
      .Include(c => c.Category)          // 關聯查詢：分類
      //.Where(c => c.ParentContentId == null)  // 只要主貼文（不要回覆）
      .OrderBy(c => c.ContentId)    // 最新的在前面
      .Select(c => new ContentViewModel
      {
        ContentId = c.ContentId,
        ParentContentId = c.ParentContentId,
        Title = c.Title,
        Body = c.Body.Substring(0, Math.Min(100, c.Body.Length)), // 只取前100字
        CreatedAt = (DateTime)c.CreatedAt,
        AuthorName = c.User.Username,
        CategoryName = c.Category.CategoryName
      })
      .ToList();
      Console.WriteLine("主貼文總數：" + contentList.Count);

      return View(contentList);
    }

    // GET: Content/Details/5
    public IActionResult Details(int id)
    {
      // 查詢主貼文
      var content = _context.Content
          .Include(c => c.User)
          .Include(c => c.Category)
          .FirstOrDefault(c => c.ContentId == id);

      if (content == null)
        return NotFound();

      // 查詢回覆列表
      var replies = _context.Content
          .Include(c => c.User)
          .Where(c => c.ParentContentId == id)  // 找出所有回覆
          .OrderBy(c => c.CreatedAt)
          .Select(c => new ReplyViewModel
          {
            ContentId = c.ContentId,
            Body = c.Body,
            AuthorName = c.User.Username,
            CreatedAt = (DateTime)c.CreatedAt
          })
          .ToList();

      // 組合成 ViewModel
      var viewModel = new ContentDetailViewModel
      {
        ContentId = content.ContentId,
        Title = content.Title,
        Body = content.Body,
        CreatedAt = (DateTime)content.CreatedAt,
        AuthorName = content.User.Username,
        CategoryName = content.Category.CategoryName,
        Replies = replies
      };

      return View(viewModel);
    }
  }
}