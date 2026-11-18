using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly IAdminDashboardService _dashboard;

        public HomeController(IAdminDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _dashboard.GetDashboardAsync());
        }
    }
}
