using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.PointShop
{
    public class AdminShippedService : IAdminShippedService
    {
        // 欄位長度（見 docs/database/schema.sql）
        private const int NameMax = 50, AddressMax = 50, PhoneMax = 50, RemarkMax = 300;

        private readonly incodedatabaseContext _context;

        public AdminShippedService(incodedatabaseContext context)
        {
            _context = context;
        }

        public Task<List<ShippedDetail>> GetShippedListAsync()
            => _context.ShippedDetail.Include(e => e.Order).AsNoTracking().ToListAsync();

        public Task<ShippedDetail?> GetShippedAsync(int shippedDetailId)
            => _context.ShippedDetail.AsNoTracking().FirstOrDefaultAsync(m => m.ShippedDetailId == shippedDetailId);

        private static void Validate(ShippedDetail form)
        {
            if (string.IsNullOrWhiteSpace(form.RecipientName) || string.IsNullOrWhiteSpace(form.RecipientPhone)
                || string.IsNullOrWhiteSpace(form.RecipientAddress))
                throw new BusinessException("請填寫完整的收件人姓名、電話與地址");
            // 原本沒有檢查長度：超過欄位長度時存檔 500
            if (form.RecipientName.Trim().Length > NameMax)
                throw new BusinessException($"收件人姓名最多 {NameMax} 字");
            if (form.RecipientAddress.Trim().Length > AddressMax)
                throw new BusinessException($"收件地址最多 {AddressMax} 字");
            if (form.RecipientPhone.Trim().Length > PhoneMax)
                throw new BusinessException($"收件人電話最多 {PhoneMax} 字");
            if ((form.Remark?.Length ?? 0) > RemarkMax)
                throw new BusinessException($"備註最多 {RemarkMax} 字");
            if (form.IsFailed && form.IsCompleted)
                throw new BusinessException("寄送不能同時是「失敗」與「完成」");
        }

        public async Task<int> CreateShippedAsync(ShippedDetail form)
        {
            Validate(form);

            await using var tx = await _context.Database.BeginTransactionAsync();

            // 先對訂單做一次「不改變內容」的條件更新：鎖住該筆訂單到交易結束，並在同一個語句判斷「存在且未取消」。
            // 同時進行的取消要等這個交易完成，不會出現「檢查時還沒取消、新增時已經取消」
            var locked = await _context.Orders
                .Where(o => o.OrderId == form.OrderId && o.Status != OrderStatus.Canceled)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, o => o.Status));
            if (locked == 0)
            {
                var exists = await _context.Orders.AnyAsync(o => o.OrderId == form.OrderId);
                throw new BusinessException(exists ? "已取消的訂單不能新增寄送資料" : $"找不到訂單 {form.OrderId}"); // 原本撞外鍵 → 500
            }

            var shipped = new ShippedDetail { OrderId = form.OrderId, IsFailed = false, IsCompleted = false };
            CopyFields(shipped, form);
            _context.ShippedDetail.Add(shipped);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return shipped.ShippedDetailId;
        }

        public async Task<bool> UpdateShippedAsync(int shippedDetailId, ShippedDetail form)
        {
            var shipped = await _context.ShippedDetail.FirstOrDefaultAsync(s => s.ShippedDetailId == shippedDetailId);
            if (shipped == null) return false;

            Validate(form);
            // 所屬訂單不可變更（原本整個實體由表單綁定，隱藏 / 唯讀欄位的 OrderId 可以被改掉）
            CopyFields(shipped, form);
            shipped.IsFailed = form.IsFailed;
            shipped.IsCompleted = form.IsCompleted;
            await _context.SaveChangesAsync();
            return true;
        }

        private static void CopyFields(ShippedDetail target, ShippedDetail form)
        {
            target.RecipientName = form.RecipientName.Trim();
            target.RecipientPhone = form.RecipientPhone.Trim();
            target.RecipientAddress = form.RecipientAddress.Trim();
            target.ShippedDate = form.ShippedDate;
            target.ArrivalDate = form.ArrivalDate;
            target.Remark = form.Remark;
        }

        public async Task<bool> DeleteShippedAsync(int shippedDetailId)
        {
            // 條件刪除：訂單狀態在同一個語句判斷，檢查與刪除之間訂單被改成已出貨時不會刪掉出貨證明
            var deleted = await _context.ShippedDetail
                .Where(s => s.ShippedDetailId == shippedDetailId
                    && s.Order.Status != OrderStatus.Shipped && s.Order.Status != OrderStatus.Completed)
                .ExecuteDeleteAsync();
            if (deleted > 0) return true;

            var status = await _context.ShippedDetail.AsNoTracking()
                .Where(s => s.ShippedDetailId == shippedDetailId).Select(s => s.Order.Status).FirstOrDefaultAsync();
            if (status == null) return false;
            throw new BusinessException($"訂單狀態為「{status}」，寄送紀錄要保留作為出貨證明，不能刪除");
        }
    }
}
