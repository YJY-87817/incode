using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Contents
{
    /// <summary>後台：每篇內容與最近一次修改前的內容對照（唯讀）</summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ContentEditController : Controller
    {
        private readonly IAdminContentService _contents;

        public ContentEditController(IAdminContentService contents)
        {
            _contents = contents;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _contents.GetLatestEditsAsync());
        }
    }
}
