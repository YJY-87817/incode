using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatusController : ControllerBase
    {
        private readonly IStatusService _statusService;

        public StatusController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        // 取得自己的等級 / 經驗 / 點數（沒有紀錄時回傳預設值，不寫入資料庫）
        [HttpGet]
        public async Task<ActionResult<UserStatus>> GetMyStatus()
            => Ok(await _statusService.GetUserStatusAsync(User.GetId()));
    }
}
