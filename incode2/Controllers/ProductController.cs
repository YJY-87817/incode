using incode2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Controllers
{
    public class ProductController : Controller
    {
        private readonly incodedatabaseContext _context;

        // 建構函式注入
        public ProductController(incodedatabaseContext context)
        {
            _context = context;
        }
        // GET: Product
        public ActionResult Index()
        {
            return View(_context.Product);
        }
       
        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // PUT: Product/Edit/5
        [HttpPut]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // POST: Product/Delete/5
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
