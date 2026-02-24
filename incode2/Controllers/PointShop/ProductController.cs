using incode2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // 用於取得 wwwroot 路徑

        // 建構函式注入
        public ProductController(incodedatabaseContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; // 用於取得 wwwroot 路徑
        }

        // GET: Product
        [HttpGet]
        public ActionResult Index()
        {
            return View(_context.Product);
        }

        // GET: Product/Create 顯示頁面
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create 存入資料庫
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

        // GET: Product/Edit/5
        [Route("/Product/Edit/{ProductId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int ProductId)
        {
            var product = await _context.Product.FindAsync(ProductId);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }


        // PUT: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ProductId, Product product, IFormFile? imageFile)
        {
            //  先從資料庫抓取原始資料（不追蹤，避免與後續 Update 衝突）
            var originalProduct = await _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == ProductId);
            if (ProductId != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 如果使用者上傳了新圖片
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/product");

                        // 1. 刪除舊圖片 (如果有舊圖的話)
                        if (!string.IsNullOrEmpty(product.ImageName))
                        {
                            string oldPath = Path.Combine(uploadsFolder, product.ImageName);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        // 2. 儲存新圖片 (同樣限制在 50 字元內)
                        string extension = Path.GetExtension(imageFile.FileName);
                        string newFileName = $"{DateTime.Now:yyyyMMddHHss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";
                        string newPath = Path.Combine(uploadsFolder, newFileName);

                        using (var fileStream = new FileStream(newPath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        // 3. 更新 Model 中的檔名
                        product.ImageName = newFileName;
                    }

                    product.UpdatedAt = DateTime.Now; // 更新修改時間

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        private bool ProductExists(int ProductId)
        {
            return _context.Product.Any(e => e.ProductId == ProductId);
        }
    }
}
