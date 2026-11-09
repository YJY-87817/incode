using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly UploadStorage _storage; // 商品圖片存放處（網站目錄外，網址仍是 /img/product/xxx）

        // 建構函式注入
        public ProductController(incodedatabaseContext context, UploadStorage storage)
        {
            _context = context;
            _storage = storage;
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
            ValidateImage(imageFile);
            if (ModelState.IsValid)
            {
                // --- 圖片處理開始 ---
                if (imageFile != null && imageFile.Length > 0)
                {
                    // 1. 產生唯一檔名 (避免使用者上傳同名檔案導致覆蓋)
                    //  取得副檔名 (例如 .jpg)
                    string extension = ImageFileValidator.NormalizedExtension(imageFile);

                    // 產生精簡檔名 (總長度約 20~30 字元，遠低於 50)
                    // 使用時間戳記 + 4位隨機數
                    string shortFileName = $"{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";

                    //  檢查最終長度（保險起見）
                    if (shortFileName.Length > 50)
                    {
                        // 如果檔名還是太長，強制截斷或更換命名邏輯
                        shortFileName = Guid.NewGuid().ToString().Substring(0, 20) + extension;
                    }
                    // 2. 儲存檔案（UploadStorage）
                    await _storage.SaveAsync(UploadFolders.Products, shortFileName, imageFile);

                    // 3. 將檔名存入 Model 的 ImageName 屬性
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
            if (ProductId != product.ProductId || originalProduct == null) return NotFound();

            ValidateImage(imageFile);
            if (ModelState.IsValid)
            {
                try
                {
                    // 如果使用者上傳了新圖片
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // 1. 刪除舊圖片：以資料庫內的檔名為準 (不信任表單傳入的 ImageName，避免路徑穿越刪除任意檔案)，
                        //    且不刪除共用的預設圖
                        var oldImageName = Path.GetFileName(originalProduct.ImageName ?? "");
                        if (!string.IsNullOrEmpty(oldImageName) &&
                            !oldImageName.Equals(DefaultImageName, StringComparison.OrdinalIgnoreCase))
                        {
                            // 新舊兩個位置（上傳資料夾 / 改版前的 wwwroot）都會找
                            _storage.Delete(UploadFolders.Products, oldImageName);
                        }

                        // 2. 儲存新圖片 (同樣限制在 50 字元內)
                        string extension = ImageFileValidator.NormalizedExtension(imageFile);
                        string newFileName = $"{DateTime.Now:yyyyMMddHHss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";
                        await _storage.SaveAsync(UploadFolders.Products, newFileName, imageFile);

                        // 3. 更新 Model 中的檔名
                        product.ImageName = newFileName;
                    }
                    else
                    {
                        // 沒有上傳新圖時沿用資料庫內的檔名
                        product.ImageName = originalProduct.ImageName;
                    }

                    product.CreatedAt = originalProduct.CreatedAt; // 建立時間不可由表單修改

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

        private const string DefaultImageName = "NoImage.png";

        // 有上傳圖片時檢查格式與檔頭，不通過則加入 ModelState 錯誤
        private void ValidateImage(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return;
            var error = ImageFileValidator.Validate(imageFile);
            if (error != null) ModelState.AddModelError("imageFile", error);
        }

        private bool ProductExists(int ProductId)
        {
            return _context.Product.Any(e => e.ProductId == ProductId);
        }
    }
}
