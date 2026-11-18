using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.PointShop
{
    public class AdminProductService : IAdminProductService
    {
        private const int NameMax = 100, DescriptionMax = 500, TypeMax = 100, SeriesMax = 100;
        public const int MaxProductPoint = 1_000_000;
        public const int MaxStock = 1_000_000;
        public const int MaxStockAdjustment = 100_000;
        private const string DefaultImageName = "NoImage.png";

        private readonly incodedatabaseContext _context;
        private readonly UploadStorage _storage; // 商品圖片存放處（網站目錄外，網址仍是 /img/product/xxx）

        public AdminProductService(incodedatabaseContext context, UploadStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        public Task<List<Product>> GetProductsAsync() => _context.Product.AsNoTracking().ToListAsync();

        public Task<Product?> GetProductAsync(int productId)
            => _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);

        private static void ValidateProduct(Product form, IFormFile? image, bool checkStock)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
                throw new BusinessException("請填寫商品名稱");
            if (form.Name.Trim().Length > NameMax)
                throw new BusinessException($"商品名稱最多 {NameMax} 字");
            if ((form.Description?.Length ?? 0) > DescriptionMax)
                throw new BusinessException($"商品描述最多 {DescriptionMax} 字");
            if ((form.Type?.Length ?? 0) > TypeMax)
                throw new BusinessException($"商品類別最多 {TypeMax} 字");
            if ((form.Series?.Length ?? 0) > SeriesMax)
                throw new BusinessException($"系列最多 {SeriesMax} 字");
            if (form.Point < 0 || form.Point > MaxProductPoint)
                throw new BusinessException($"所需點數需在 0～{MaxProductPoint:N0} 之間");
            if (checkStock && (form.Stock < 0 || form.Stock > MaxStock))
                throw new BusinessException($"庫存需在 0～{MaxStock:N0} 之間");

            if (image != null && image.Length > 0)
            {
                var error = ImageFileValidator.Validate(image);
                if (error != null) throw new BusinessException(error);
            }
        }

        // 檔名由伺服器產生（時間戳記 + 隨機碼），不使用上傳的檔名
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..8]}{ImageFileValidator.NormalizedExtension(image)}";
            await _storage.SaveAsync(UploadFolders.Products, fileName, image);
            return fileName;
        }

        public async Task<int> CreateProductAsync(Product form, IFormFile? image)
        {
            ValidateProduct(form, image, checkStock: true);

            string? savedImage = null;
            try
            {
                var now = DateTime.Now;
                var product = new Product
                {
                    Name = form.Name.Trim(),
                    Description = form.Description,
                    Type = form.Type,
                    Series = form.Series,
                    IsPhysical = form.IsPhysical,
                    Point = form.Point,
                    Stock = form.Stock,
                    OnSale = form.OnSale,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                if (image != null && image.Length > 0)
                    product.ImageName = savedImage = await SaveImageAsync(image);

                _context.Product.Add(product);
                await _context.SaveChangesAsync();
                return product.ProductId;
            }
            catch
            {
                // 資料庫寫入失敗：不留下沒有商品使用的圖片檔
                if (savedImage != null) _storage.Delete(UploadFolders.Products, savedImage);
                throw;
            }
        }

        public async Task<bool> UpdateProductAsync(int productId, Product form, IFormFile? image, int stockDelta)
        {
            var product = await _context.Product.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return false;

            ValidateProduct(form, image, checkStock: false);
            // 直接比較範圍：Math.Abs(int.MinValue) 會丟 OverflowException（500）
            if (stockDelta < -MaxStockAdjustment || stockDelta > MaxStockAdjustment)
                throw new BusinessException($"單次調整庫存最多 ±{MaxStockAdjustment:N0}");

            string? newImage = null;
            string? oldImage = null;
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (stockDelta != 0)
                {
                    // 條件更新：不會蓋掉同時下單扣掉的庫存，也不會扣到負數 / 超過上限
                    var updated = await _context.Product
                        .Where(p => p.ProductId == productId && p.Stock + stockDelta >= 0 && p.Stock + stockDelta <= MaxStock)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + stockDelta));
                    if (updated == 0)
                    {
                        var current = await _context.Product.AsNoTracking().Where(p => p.ProductId == productId)
                            .Select(p => p.Stock).FirstAsync();
                        throw new BusinessException(stockDelta < 0
                            ? $"庫存不足：目前庫存 {current:N0}，不能減少 {-stockDelta:N0}"
                            : $"調整後庫存不能超過 {MaxStock:N0}（目前庫存 {current:N0}）");
                    }
                }

                product.Name = form.Name.Trim();
                product.Description = form.Description;
                product.Type = form.Type;
                product.Series = form.Series;
                product.IsPhysical = form.IsPhysical;
                product.Point = form.Point;
                product.OnSale = form.OnSale;
                product.UpdatedAt = DateTime.Now;

                if (image != null && image.Length > 0)
                {
                    // 刪除舊圖片以資料庫內的檔名為準（不信任表單傳入的 ImageName，避免路徑穿越刪除任意檔案），不刪除共用的預設圖
                    var old = Path.GetFileName(product.ImageName ?? "");
                    if (!string.IsNullOrEmpty(old) && !old.Equals(DefaultImageName, StringComparison.OrdinalIgnoreCase))
                        oldImage = old;
                    product.ImageName = newImage = await SaveImageAsync(image);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                if (newImage != null) _storage.Delete(UploadFolders.Products, newImage);
                throw;
            }

            // 資料庫已更新才刪除舊圖片（原本先刪檔再存資料庫，存檔失敗會留下指向已刪除檔案的商品）
            if (oldImage != null) _storage.Delete(UploadFolders.Products, oldImage);
            return true;
        }
    }
}
