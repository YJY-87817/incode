
namespace incode.MVC.Services.Users
{
    public interface IInventoryService
    {
        Task<List<InventoryItemDto>> GetMyInventoryAsync(int userId);
        Task<int> UseItemAsync(int userId, int itemId);

        Task AddItemAsync(int userId, int itemId, int quantity); // 獲得道具：只供伺服器端流程呼叫，不要開放成會員 API
        Task<bool> RemoveItemAsync(int userId, int inventoryId); // 丟棄道具 (刪除)
    }
}
