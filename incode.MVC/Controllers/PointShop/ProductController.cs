using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IAdminShopService _shop;

        public ProductController(IAdminShopService shop)
        {
            _shop = shop;
        }

        // 表單可以設定的欄位（建立時間、圖片檔名由系統設定；庫存只能在建立時填、之後用「調整庫存」）
        private const string CreateFields = "Name,Description,Type,Series,IsPhysical,Point,Stock,OnSale";
        private const string EditFields = "ProductId,Name,Description,Type,Series,IsPhysical,Point,OnSale";

        // GET: Product
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await _shop.GetProductsAsync());
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
        public async Task<IActionResult> Create([Bind(CreateFields)] Product product, IFormFile? imageFile)
        {
            try
            {
                await _shop.CreateProductAsync(product, imageFile);
                TempData["SuccessMessage"] = "商品已新增";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(product);
            }
        }

        // GET: Product/Edit/5
        [Route("/Product/Edit/{ProductId}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int ProductId)
        {
            var product = await _shop.GetProductAsync(ProductId);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Product/Edit/5
        // stockDelta：庫存增減（正數增加、負數減少），以原子更新套用
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ProductId, [Bind(EditFields)] Product product, IFormFile? imageFile, int stockDelta = 0)
        {
            if (ProductId != product.ProductId) return NotFound();

            try
            {
                if (!await _shop.UpdateProductAsync(ProductId, product, imageFile, stockDelta)) return NotFound();
                TempData["SuccessMessage"] = "商品已更新";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                // 重新顯示時帶出資料庫目前的庫存、圖片與建立時間（表單沒有送出這些）
                var current = await _shop.GetProductAsync(ProductId);
                if (current == null) return NotFound();
                product.Stock = current.Stock;
                product.ImageName = current.ImageName;
                product.CreatedAt = current.CreatedAt;
                product.UpdatedAt = current.UpdatedAt;
                return View(product);
            }
        }
    }
}
