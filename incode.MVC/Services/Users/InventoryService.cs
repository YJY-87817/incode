using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Users
{
    public class InventoryService : IInventoryService
    {
        private readonly incodedatabaseContext _context;

        public InventoryService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 1. 取得背包內容
        public async Task<List<InventoryItemDto>> GetMyInventoryAsync(int userId)
        {
            return await _context.UserInventory
                .Where(i => i.UserId == userId && i.Quantity > 0)
                // .Include(i => i.Item) // 如果未來有 Item 表要記得 Include
                .Select(i => new InventoryItemDto
                {
                    InventoryId = i.InventoryId,
                    ItemId = i.ItemId ?? 0, // 若 i.ItemId 為 null，則給予預設值 0
                    Quantity = i.Quantity ?? 0 // 若 i.Quantity 為 null，則給予預設值 0
                })
                .ToListAsync();
        }

        // 2. 使用道具
        public async Task<int> UseItemAsync(int userId, int itemId)
        {
            // 原子扣減：以條件更新（數量 >= 1 才扣）。原本讀出數量再存回，同時使用多次會都成功、只扣一次
            // 同一道具可能有多筆（AddItemAsync 沒有唯一限制），一次只扣一筆
            var inventoryId = await _context.UserInventory
                .Where(i => i.UserId == userId && i.ItemId == itemId && i.Quantity >= 1)
                .OrderBy(i => i.InventoryId)
                .Select(i => (int?)i.InventoryId)
                .FirstOrDefaultAsync();
            if (inventoryId == null) return -1; // 道具不存在或數量不足

            int updated = await _context.UserInventory
                .Where(i => i.InventoryId == inventoryId && i.Quantity >= 1)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => i.Quantity - 1));
            if (updated == 0) return -1; // 同時有其他請求先用掉了

            // 如果數量歸零是否刪除資料，視需求而定，這裡保留
            var remaining = await _context.UserInventory
                .AsNoTracking()
                .Where(i => i.InventoryId == inventoryId)
                .Select(i => i.Quantity)
                .FirstOrDefaultAsync();
            return remaining ?? 0;
        }
        public async Task AddItemAsync(int userId, int itemId, int quantity)
        {
            // 1. 先檢查背包有沒有這個東西
            var existingItem = await _context.UserInventory
                .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId);

            if (existingItem != null)
            {
                // 2. 有的話，就增加數量 (這就是一種 Modify)
                existingItem.Quantity += quantity;
            }
            else
            {
                // 3. 沒有的話，就新增一筆
                var newItem = new UserInventory
                {
                    UserId = userId,
                    ItemId = itemId,
                    Quantity = quantity
                };
                _context.UserInventory.Add(newItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveItemAsync(int userId, int inventoryId) // 注意這裡是用 inventoryId 刪除比較準
        {
            var item = await _context.UserInventory
                .FirstOrDefaultAsync(i => i.UserId == userId && i.InventoryId == inventoryId);

            if (item == null) return false;

            _context.UserInventory.Remove(item); // 直接從資料庫刪除
            await _context.SaveChangesAsync();
            return true;
        }
    }
}