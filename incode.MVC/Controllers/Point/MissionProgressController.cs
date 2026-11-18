using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Point
{
    /// <summary>
    /// 後台任務進度：只能查詢。任務進度只由伺服器端觸發，原本的新增 / 修改 / 刪除可以把進度改成 completed 讓會員領獎、
    /// 或刪掉已領獎的一次性任務進度讓會員再領一次。要補點數請用「手動調整點數」。
    /// </summary>
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class MissionProgressController : Controller
    {
        private readonly IAdminMissionService _missions;

        public MissionProgressController(IAdminMissionService missions)
        {
            _missions = missions;
        }

        // GET: MissionProgress
        public async Task<IActionResult> Index(int? MissionId, int? UserId)
        {
            var (items, title) = await _missions.GetProgressAsync(MissionId, UserId);
            ViewBag.PageTitle = title;
            return View(items);
        }

        // GET: MissionProgress/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var missionProgress = await _missions.GetProgressByIdAsync(id.Value);
            if (missionProgress == null) return NotFound();
            return View(missionProgress);
        }
    }
}
