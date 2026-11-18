using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.PointShop
{
    public class AdminShopService : IAdminShopService
    {
        // 欄位長度（見 docs/database/schema.sql）
        private const int ShippedNameMax = 50, ShippedAddressMax = 50, ShippedPhoneMax = 50, ShippedRemarkMax = 300;
        private const int ProductNameMax = 100, ProductDescriptionMax = 500, ProductTypeMax = 100, ProductSeriesMax = 100;
        public const int MaxProductPoint = 1_000_000;
        public const int MaxStock = 1_000_000;
        public const int MaxStockAdjustment = 100_000;
        private const string DefaultImageName = "NoImage.png";

        private readonly incodedatabaseContext _context;
        private readonly IOrderService _orders;
        private readonly UploadStorage _storage; // 商品圖片存放處（網站目錄外，網址仍是 /img/product/xxx）

        public AdminShopService(incodedatabaseContext context, IOrderService orders, UploadStorage storage)
        {
            _context = context;
            _orders = orders;
            _storage = storage;
        }

        // =====================================================================
        // 訂單
        // =====================================================================

        public Task<List<Orders>> GetOrdersAsync() => _context.Orders.AsNoTracking().ToListAsync();

        public async Task<OrderViewModel?> GetOrderForEditAsync(int orderId)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return null;

            return new OrderViewModel
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                TotalPoint = order.TotalPoint,
                Status = order.Status,
                Remark = order.Remark,
                orderdetails = await _context.Orderdetail.AsNoTracking().Where(od => od.OrderId == orderId).ToListAsync(),
            };
        }

        public Task<List<Product>> GetProductOptionsAsync()
            => _context.Product.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

        // 表單明細只取商品與數量
        private static List<OrderItemRequest> ToItems(IEnumerable<Orderdetail>? details)
            => (details ?? Enumerable.Empty<Orderdetail>())
                .Select(d => new OrderItemRequest { ProductId = d.ProductId, Quantity = d.Quantity })
                .ToList();

        public async Task<ResultDTO<int>> CreateOrderAsync(OrderViewModel form)
        {
            if (!await _context.User.AnyAsync(u => u.UserId == form.UserId))
                return ResultDTO<int>.Failure($"找不到會員 {form.UserId}", 404);

            return await _orders.CreateOrderAsync(
                form.UserId,
                ToItems(form.orderdetails),
                recipient: null,               // 寄送資料由出貨管理另外建立
                remark: form.Remark,
                requireRecipient: false,
                status: form.Status ?? OrderStatus.Pending);
        }

        public async Task<ResultDTO<int>> UpdateOrderAsync(int orderId, OrderViewModel form)
        {
            var currentStatus = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId).Select(o => o.Status).FirstOrDefaultAsync();
            if (currentStatus == null)
                return ResultDTO<int>.Failure("找不到該訂單", 404);

            if (!OrderStatus.IsValid(form.Status))
                return ResultDTO<int>.Failure("訂單狀態錯誤");

            // 已取消的訂單庫存與點數都已退還，不可改回其他狀態 (否則等於免費取得商品)
            if (currentStatus == OrderStatus.Canceled)
            {
                if (form.Status != OrderStatus.Canceled)
                    return ResultDTO<int>.Failure("已取消的訂單不能恢復，請重新建立訂單", 409);

                await UpdateRemarkAsync(orderId, form.Remark);
                return ResultDTO<int>.Success(orderId, "訂單備註已更新！");
            }

            // 改為取消：退還庫存與點數 (會員 / 明細不允許在此同時修改)
            if (form.Status == OrderStatus.Canceled)
            {
                var cancel = await _orders.CancelOrderAsync(orderId);
                if (!cancel.Ok) return cancel;

                await UpdateRemarkAsync(orderId, form.Remark);
                return ResultDTO<int>.Success(orderId, "訂單已取消，庫存與點數已退還！");
            }

            // 修改明細與狀態：庫存、總點數、點數差額、狀態都在同一個交易（鎖內）完成。
            // 原本狀態是明細修改完成之後才另外更新：期間被取消的訂單會被改回「已出貨」，庫存與點數卻已退還
            var result = await _orders.UpdateOrderItemsAsync(
                orderId, ToItems(form.orderdetails), form.Remark ?? "", requiredStatus: null, newStatus: form.Status);
            if (!result.Ok) return result;

            return ResultDTO<int>.Success(orderId, "訂單修改成功！");
        }

        private Task UpdateRemarkAsync(int orderId, string? remark)
            => _context.Orders
                .Where(o => o.OrderId == orderId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Remark, remark ?? ""));

        // =====================================================================
        // 寄送紀錄
        // =====================================================================

        public Task<List<ShippedDetail>> GetShippedListAsync()
            => _context.ShippedDetail.Include(e => e.Order).AsNoTracking().ToListAsync();

        public async Task<List<AdminShippedRow>> GetShippedRowsAsync()
        {
            var items = await _context.ShippedDetail.Include(e => e.Order).AsNoTracking().ToListAsync();
            return items.Select(item => new AdminShippedRow
            {
                ShippedDetailId = item.ShippedDetailId,
                OrderId = item.OrderId,
                OrderStatus = item.Order.Status,
                RecipientName = item.RecipientName,
                RecipientAddress = item.RecipientAddress,
                RecipientPhone = item.RecipientPhone,
                ShippedDate = item.ShippedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ShippedDateFormatted = item.ShippedDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                ArrivalDate = item.ArrivalDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                ArrivalDateFormatted = item.ArrivalDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                IsFailed = item.IsFailed,
                Remark = item.Remark,
                IsCompleted = item.IsCompleted,
            }).ToList();
        }

        public Task<ShippedDetail?> GetShippedAsync(int shippedDetailId)
            => _context.ShippedDetail.AsNoTracking().FirstOrDefaultAsync(m => m.ShippedDetailId == shippedDetailId);

        private static void ValidateShipped(ShippedDetail form)
        {
            if (string.IsNullOrWhiteSpace(form.RecipientName) || string.IsNullOrWhiteSpace(form.RecipientPhone)
                || string.IsNullOrWhiteSpace(form.RecipientAddress))
                throw new BusinessException("請填寫完整的收件人姓名、電話與地址");
            // 原本沒有檢查長度：超過欄位長度時存檔 500
            if (form.RecipientName.Trim().Length > ShippedNameMax)
                throw new BusinessException($"收件人姓名最多 {ShippedNameMax} 字");
            if (form.RecipientAddress.Trim().Length > ShippedAddressMax)
                throw new BusinessException($"收件地址最多 {ShippedAddressMax} 字");
            if (form.RecipientPhone.Trim().Length > ShippedPhoneMax)
                throw new BusinessException($"收件人電話最多 {ShippedPhoneMax} 字");
            if ((form.Remark?.Length ?? 0) > ShippedRemarkMax)
                throw new BusinessException($"備註最多 {ShippedRemarkMax} 字");
            if (form.IsFailed && form.IsCompleted)
                throw new BusinessException("寄送不能同時是「失敗」與「完成」");
        }

        public async Task<int> CreateShippedAsync(ShippedDetail form)
        {
            ValidateShipped(form);

            var orderStatus = await _context.Orders.AsNoTracking()
                .Where(o => o.OrderId == form.OrderId).Select(o => o.Status).FirstOrDefaultAsync()
                ?? throw new BusinessException($"找不到訂單 {form.OrderId}"); // 原本撞外鍵 → 500
            if (orderStatus == OrderStatus.Canceled)
                throw new BusinessException("已取消的訂單不能新增寄送資料");

            var shipped = new ShippedDetail { OrderId = form.OrderId, IsFailed = false, IsCompleted = false };
            CopyShippedFields(shipped, form);
            _context.ShippedDetail.Add(shipped);
            await _context.SaveChangesAsync();
            return shipped.ShippedDetailId;
        }

        public async Task<bool> UpdateShippedAsync(int shippedDetailId, ShippedDetail form)
        {
            var shipped = await _context.ShippedDetail.FirstOrDefaultAsync(s => s.ShippedDetailId == shippedDetailId);
            if (shipped == null) return false;

            ValidateShipped(form);
            // 所屬訂單不可變更（原本整個實體由表單綁定，隱藏 / 唯讀欄位的 OrderId 可以被改掉）
            CopyShippedFields(shipped, form);
            shipped.IsFailed = form.IsFailed;
            shipped.IsCompleted = form.IsCompleted;
            await _context.SaveChangesAsync();
            return true;
        }

        private static void CopyShippedFields(ShippedDetail target, ShippedDetail form)
        {
            target.RecipientName = form.RecipientName.Trim();
            target.RecipientPhone = form.RecipientPhone.Trim();
            target.RecipientAddress = form.RecipientAddress.Trim();
            target.ShippedDate = form.ShippedDate;
            target.ArrivalDate = form.ArrivalDate;
            target.Remark = form.Remark;
        }

        public async Task DeleteShippedAsync(int shippedDetailId)
        {
            var shipped = await _context.ShippedDetail.Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.ShippedDetailId == shippedDetailId);
            if (shipped == null) return;

            if (shipped.Order.Status is OrderStatus.Shipped or OrderStatus.Completed)
                throw new BusinessException($"訂單狀態為「{shipped.Order.Status}」，寄送紀錄要保留作為出貨證明，不能刪除");

            _context.ShippedDetail.Remove(shipped);
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // 商品
        // =====================================================================

        public Task<List<Product>> GetProductsAsync() => _context.Product.AsNoTracking().ToListAsync();

        public Task<Product?> GetProductAsync(int productId)
            => _context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);

        private static void ValidateProduct(Product form, IFormFile? image, bool checkStock)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
                throw new BusinessException("請填寫商品名稱");
            if (form.Name.Trim().Length > ProductNameMax)
                throw new BusinessException($"商品名稱最多 {ProductNameMax} 字");
            if ((form.Description?.Length ?? 0) > ProductDescriptionMax)
                throw new BusinessException($"商品描述最多 {ProductDescriptionMax} 字");
            if ((form.Type?.Length ?? 0) > ProductTypeMax)
                throw new BusinessException($"商品類別最多 {ProductTypeMax} 字");
            if ((form.Series?.Length ?? 0) > ProductSeriesMax)
                throw new BusinessException($"系列最多 {ProductSeriesMax} 字");
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
            if (Math.Abs(stockDelta) > MaxStockAdjustment)
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
