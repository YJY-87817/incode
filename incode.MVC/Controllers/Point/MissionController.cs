using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class MissionController : Controller
    {
        private readonly IAdminMissionService _missions;

        public MissionController(IAdminMissionService missions)
        {
            _missions = missions;
        }

        // GET: Mission
        public async Task<IActionResult> Index()
        {
            return View(await _missions.GetMissionsAsync());
        }

        // GET: Mission/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var mission = await _missions.GetMissionAsync(id.Value);
            if (mission == null) return NotFound();
            return View(mission);
        }

        // GET: Mission/Create
        public IActionResult Create()
        {
            return View(new Mission { IsActive = true, ResetType = "none", Difficulty = "中等", TargetCount = 1 });
        }

        // POST: Mission/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(MissionFormFields)] Mission mission)
        {
            try
            {
                await _missions.CreateMissionAsync(mission);
                TempData["SuccessMessage"] = "平台任務已建立";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(mission);
            }
        }

        // GET: Mission/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var mission = await _missions.GetMissionAsync(id.Value);
            if (mission == null) return NotFound();
            return View(mission);
        }

        // POST: Mission/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(MissionFormFields)] Mission mission)
        {
            try
            {
                if (!await _missions.UpdateMissionAsync(id, mission)) return NotFound();
                TempData["SuccessMessage"] = "平台任務已更新";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                // 重新顯示時保留系統欄位（參與人數、上次重置時間）
                var current = await _missions.GetMissionAsync(id);
                if (current == null) return NotFound();
                mission.MissionId = id;
                mission.ParticipantCount = current.ParticipantCount;
                mission.LastResetAt = current.LastResetAt;
                return View(mission);
            }
        }

        // 表單可以設定的欄位；建立時間、參與人數、上次重置時間由系統維護
        private const string MissionFormFields =
            "MissionName,MissionDetail,TargetType,TargetCount,RewardPoints,RewardExp,Category,Difficulty,EstimatedDuration,ResetType,IsFeatured,IsActive";

        // GET: Mission/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var mission = await _missions.GetMissionAsync(id.Value);
            if (mission == null) return NotFound();
            return View(mission);
        }

        // POST: Mission/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _missions.DeleteMissionAsync(id);
                TempData["SuccessMessage"] = "平台任務已刪除";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
