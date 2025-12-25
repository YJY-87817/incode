using Microsoft.AspNetCore.Mvc;

namespace incode2.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
