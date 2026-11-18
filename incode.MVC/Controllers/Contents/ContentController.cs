using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Contents
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ContentController : Controller
    {
        private readonly IAdminContentService _contents;

        public ContentController(IAdminContentService contents)
        {
            _contents = contents;
        }

        // GET: Content
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _contents.GetContentsAsync());
        }

        // GET: Content/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var viewModel = await _contents.GetContentDetailAsync(id);
            if (viewModel == null) return NotFound();
            return View(viewModel);
        }
    }
}
