using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointLogApiController : ControllerBase
    {
        private readonly IPointLogService _pointLogService;

        public PointLogApiController(IPointLogService pointLogService)
        {
            _pointLogService = pointLogService;
        }

        // 點數只能由伺服器端流程異動 (任務領獎、委託任務最佳解答、打賞、儲值、商城)，
        // 不提供讓前端自訂點數的 API；也不提供列出所有人紀錄的 API (後台有 PointLog 管理頁)

        // GET: api/PointLogApi/user/my-logs  取得自己的點數紀錄
        [HttpGet("user/my-logs")]
        [Authorize]
        public async Task<IEnumerable<PointLogDTO>> GetPointLogByUser()
        {
            int userId = User.GetId();
            return await _pointLogService.GetUserLogsAsync(userId);
        }
    }
}
