using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Contents
{
    /// <summary>後台檢舉清單：駁回、下架（官方移除）、恢復上架。邏輯在 ContentReportService。</summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ContentReportController : Controller
    {
        private readonly IContentReportService _reports;

        public ContentReportController(IContentReportService reports)
        {
            _reports = reports;
        }

        // GET: ContentReport
        public async Task<IActionResult> Index()
        {
            return View(await _reports.GetIndexAsync());
        }

        // POST: ContentReport/Dismiss/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Dismiss(int id, string? note)
            => RunAsync(() => _reports.DismissAsync(id, User.GetId(), note), $"已駁回內容 #{id} 的檢舉");

        // POST: ContentReport/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Remove(int id, string? note)
            => RunAsync(() => _reports.RemoveAsync(id, User.GetId(), note), $"已下架內容 #{id}");

        // POST: ContentReport/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Restore(int id, string? note)
            => RunAsync(() => _reports.RestoreAsync(id, User.GetId(), note), $"已恢復上架內容 #{id}");

        private async Task<IActionResult> RunAsync(Func<Task> action, string successMessage)
        {
            try
            {
                await action();
                TempData["SuccessMessage"] = successMessage;
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
