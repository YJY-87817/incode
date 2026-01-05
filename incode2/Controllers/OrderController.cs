using incode2.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{

    public class OrderController : Controller
    {
    private readonly incodedatabaseContext _context;

        public OrderController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: Order
        [HttpGet]
        public ActionResult Index()
        {
            return View(_context.Orders);
        }

        // GET: Order/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
    }
}
