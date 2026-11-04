using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {        
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // GET: api/Inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMyInventory()
        {
            int userId = User.GetId();
                       
            var items = await _inventoryService.GetMyInventoryAsync(userId);

            return Ok(items);
        }

        // POST: api/Inventory/add
        // 測試用：獲得道具
        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] AddItemDto dto)
        {
            int userId = User.GetId();
            await _inventoryService.AddItemAsync(userId, dto.ItemId, dto.Quantity);
            return Ok(new { message = $"已獲得道具 ID: {dto.ItemId}, 數量: {dto.Quantity}" });
        }

        // POST: api/Inventory/use/101
        // 使用道具 (數量減少)
        [HttpPost("use/{itemId}")]
        public async Task<IActionResult> UseItem(int itemId)
        {
            int userId = User.GetId();
            int remaining = await _inventoryService.UseItemAsync(userId, itemId);

            if (remaining == -1) return BadRequest("道具不足或不存在");

            return Ok(new { message = "使用成功", remaining });
        }

        // DELETE: api/Inventory/10
        // 刪除道具 (丟棄)
        [HttpDelete("{inventoryId}")]
        public async Task<IActionResult> DeleteItem(int inventoryId)
        {
            int userId = User.GetId();
            bool result = await _inventoryService.RemoveItemAsync(userId, inventoryId);

            if (!result) return NotFound("找不到該道具");

            return Ok(new { message = "道具已丟棄" });
        }
    }
}