using incode2.DTO.user;
using incode2.Models;
using incode2.Services.user;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.user
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
            var inventoryItem = await _context.UserInventory
                .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId);

            // 檢查道具是否存在或數量不足
            if (inventoryItem == null || inventoryItem.Quantity < 1)
            {
                return -1; // 表示失敗代碼
            }

            inventoryItem.Quantity -= 1;

            // 如果數量歸零是否刪除資料，視需求而定，這裡保留

            await _context.SaveChangesAsync();

            return inventoryItem.Quantity ?? 0; // 回傳剩餘數量，若為 null 則回傳 0
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