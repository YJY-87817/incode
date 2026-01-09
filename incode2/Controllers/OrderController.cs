using incode2.Models;
using incode2.ViewModels;
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
            // 從資料庫抓出商品（可加條件例如 OnSale、Stock > 0）
            var products = _context.Product.OrderBy(p => p.Name).ToList();
            ViewBag.Products = products;
            return View();
        }

        // POST: Order/Create 新增訂單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel order)
        {
            return View();
        }
    }
}
