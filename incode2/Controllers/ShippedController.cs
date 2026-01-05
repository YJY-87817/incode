using incode2.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{
    public class ShippedController : Controller
    {
        private readonly incodedatabaseContext _context;
        public ShippedController(incodedatabaseContext context)
        {
            _context = context;
        }
        // GET: Shipped
        [HttpGet]
        public ActionResult Index()
        {
            return View(_context.ShippedDetail);
        }

        // GET: Product/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
    }
}
