using incode2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Controllers
{
    public class ProductController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // 用於取得 wwwroot 路徑

        // 建構函式注入
        public ProductController(incodedatabaseContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        // GET: Product
        public ActionResult Index()
        {
            return View(_context.Product);
        }

        // GET: Customers/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                // --- 圖片處理開始 ---
                if (imageFile != null && imageFile.Length > 0)
                {
                    // 1. 設定儲存路徑 (wwwroot/imag/product)
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/product");

                    // 確保資料夾存在
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // 2. 產生唯一檔名 (避免使用者上傳同名檔案導致覆蓋)
                    //  取得副檔名 (例如 .jpg)
                    string extension = Path.GetExtension(imageFile.FileName);

                    // 產生精簡檔名 (總長度約 20~30 字元，遠低於 50)
                    // 使用時間戳記 + 4位隨機數
                    string shortFileName = $"{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";

                    //  檢查最終長度（保險起見）
                    if (shortFileName.Length > 50)
                    {
                        // 如果檔名還是太長，強制截斷或更換命名邏輯
                        shortFileName = Guid.NewGuid().ToString().Substring(0, 20) + extension;
                    }
                    string filePath = Path.Combine(uploadsFolder, shortFileName);

                    // 3. 儲存檔案到實體路徑
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // 4. 將檔名存入 Model 的 ImageName 屬性
                    product.ImageName = shortFileName;
                }
                // --- 圖片處理結束 ---

                // 設定預設時間
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        //// GET: Customers/Edit/5
        //[HttpGet]
        //public async Task<IActionResult> Edit(string CustomerID)
        //{
        //    if (CustomerID == null)
        //    {
        //        return NotFound();  //404
        //    }

        //    var customer = await _context.Customers.FindAsync(CustomerID);
        //    if (customer == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(customer);
        //}

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
