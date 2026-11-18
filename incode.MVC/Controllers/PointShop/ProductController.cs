using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.PointShop
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IAdminProductService _products;

        public ProductController(IAdminProductService products)
        {
            _products = products;
        }

        // 表單可以設定的欄位（建立時間、圖片檔名由系統設定；庫存只能在建立時填、之後用「調整庫存」）
        private const string CreateFields = "Name,Description,Type,Series,IsPhysical,Point,Stock,OnSale";
        private const string EditFields = "ProductId,Name,Description,Type,Series,IsPhysical,Point,OnSale";

        // GET: Product
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return View(await _products.GetProductsAsync());
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
            // 數字欄位填了無法轉換的值（例如 "abc"）時，模型繫結失敗但欄位會變成 0：不能當成 0 存起來
            if (HasInvalidInput()) return View(product);

            try
            {
                await _products.CreateProductAsync(product, imageFile);
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
            var product = await _products.GetProductAsync(ProductId);
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
            if (HasInvalidInput()) return await RedisplayEditAsync(ProductId, product);

            try
            {
                if (!await _products.UpdateProductAsync(ProductId, product, imageFile, stockDelta)) return NotFound();
                TempData["SuccessMessage"] = "商品已更新";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return await RedisplayEditAsync(ProductId, product);
            }
        }

        // 重新顯示時帶出資料庫目前的庫存、圖片與建立 / 修改時間（表單沒有送出這些）
        private async Task<IActionResult> RedisplayEditAsync(int productId, Product product)
        {
            var current = await _products.GetProductAsync(productId);
            if (current == null) return NotFound();
            product.Stock = current.Stock;
            product.ImageName = current.ImageName;
            product.CreatedAt = current.CreatedAt;
            product.UpdatedAt = current.UpdatedAt;
            return View(product);
        }

        private static readonly Dictionary<string, string> FieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Point"] = "所需點數", ["Stock"] = "庫存數量", ["stockDelta"] = "調整庫存", ["IsPhysical"] = "實體商品", ["OnSale"] = "販售中",
        };

        // 模型繫結有錯誤（預設訊息是英文）：改成一則中文訊息列出有問題的欄位
        private bool HasInvalidInput()
        {
            if (ModelState.IsValid) return false;
            var fields = ModelState.Where(e => e.Value?.Errors.Count > 0)
                .Select(e => FieldNames.GetValueOrDefault(e.Key, e.Key)).Distinct().ToList();
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "下列欄位的內容格式不正確，請輸入有效的數字：" + string.Join("、", fields));
            return true;
        }
    }
}
