using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Contents
{
    /// <summary>
    /// 後台文章修改紀錄：只能查看。紀錄由會員編輯文章時自動寫入（ContentService）；
    /// 原本的新增 / 修改 / 刪除可以偽造或清除修改歷程（而且新增 / 修改頁引用不存在的欄位，一律 500）。
    /// </summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    [Route("/ContentHistory/{action=Index}/{content_history_id?}")]
    public class ContentHistoryController : Controller
    {
        private readonly IAdminContentService _contents;

        public ContentHistoryController(IAdminContentService contents)
        {
            _contents = contents;
        }

        // GET: ContentHistory
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _contents.GetHistoriesAsync());
        }

        // GET: ContentHistory/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? content_history_id)
        {
            if (content_history_id == null) return NotFound();
            var contentHistory = await _contents.GetHistoryAsync(content_history_id.Value);
            if (contentHistory == null) return NotFound();
            return View(contentHistory);
        }
    }
}
