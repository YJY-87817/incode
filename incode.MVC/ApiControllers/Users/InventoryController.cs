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

        // 原本的 POST api/Inventory/add（測試用：會員可以替自己加任意道具與數量）已移除，前端沒有使用。
        // 道具只能由伺服器端流程（例如兌換、任務獎勵）透過 IInventoryService.AddItemAsync 發放

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