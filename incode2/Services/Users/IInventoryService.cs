
namespace incode2.Services.Users
{
    public interface IInventoryService
    {
        Task<List<InventoryItemDto>> GetMyInventoryAsync(int userId);
        Task<int> UseItemAsync(int userId, int itemId);

        Task AddItemAsync(int userId, int itemId, int quantity); // 獲得道具 (方便測試用)
        Task<bool> RemoveItemAsync(int userId, int inventoryId); // 丟棄道具 (刪除)
    }
}
