using incode2.Models;
using incode2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{
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
      //這邊我成功Join兩個資料表
      var joinA = _context.Content.Include(c => c.ContentHistory);
      //這邊我成功Join兩個資料表
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
        Ishidden = d.Ishidden,
        ContentHistoryId = d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault().ContentHistoryId : 0,
        OldTitle = d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault().OldTitle : string.Empty,
        OldBody = d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault().OldBody : string.Empty,
        HistoryIshidden = d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault().Ishidden : null,
        EditedAt = d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault() != null ? d.ContentHistory.OrderByDescending(ch => ch.EditedAt).FirstOrDefault().EditedAt : null
      });
      return View(ContentEditViewModel);
    }
  }
}
