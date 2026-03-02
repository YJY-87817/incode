using incode2.Models;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers.Contents
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]

    public class ContentEditController : Controller
  {
    //DI容器注入
    private readonly incodedatabaseContext _context;

    // 建構函式注入
    public ContentEditController(incodedatabaseContext context)
    {
      _context = context;
    }
    //DI容器注入

    public async Task<IActionResult> Index()
    {
      // 從 Content 帶出 ContentHistory，供編輯畫面比較新舊內容
      var joinA = _context.Content.Include(c => c.ContentHistory);
      // 將資料庫結果投影成 ViewModel，交由 Views/ContentEdit/Index.cshtml 使用
      var ContentEditViewModel = joinA.Select(d=>new ContentEditViewModel
      {
        ContentId = d.ContentId,
        ParentContentId = d.ParentContentId,
        CategoryId = d.CategoryId,
        UserId = d.UserId,
        Title = d.Title,
        Body = d.Body,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        ContentHistoryId = d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault().ContentHistoryId : 0,
        OldTitle = d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault().OldTitle : string.Empty,
        OldBody = d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault().OldBody : string.Empty,
      });
      return View(ContentEditViewModel);
    }
  }
}
