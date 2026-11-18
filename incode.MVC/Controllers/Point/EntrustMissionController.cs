using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class EntrustMissionController : Controller
    {
        private readonly IAdminEntrustMissionService _admin;
        private readonly IEntrustMissionService _entrustMissionService;

        public EntrustMissionController(IAdminEntrustMissionService admin, IEntrustMissionService entrustMissionService)
        {
            _admin = admin;
            _entrustMissionService = entrustMissionService;
        }

        // GET: EntrustMission
        public async Task<IActionResult> Index()
        {
            return View(await _admin.GetAllAsync());
        }

        // GET: EntrustMission/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var entrustMission = await _admin.GetAsync(id.Value);
            if (entrustMission == null) return NotFound();
            return View(entrustMission);
        }

        // GET: EntrustMission/Create
        // 代會員建立委託：與前台相同走預扣流程（原本直接寫入委託、不扣點，之後選最佳解答或退款會憑空發點）
        public async Task<IActionResult> Create()
        {
            await LoadTopicsAsync(null);
            return View(new EntrustMissionCreateViewModel());
        }

        // POST: EntrustMission/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntrustMissionCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var id = await _admin.CreateAsync(vm);
                    TempData["SuccessMessage"] = $"委託 #{id} 已建立，賞金已從作者點數預扣";
                    return RedirectToAction(nameof(Index));
                }
                catch (BusinessException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            await LoadTopicsAsync(vm.ContentId);
            return View(vm);
        }

        private async Task LoadTopicsAsync(int? selected)
        {
            var topics = (await _admin.GetEligibleTopicsAsync()).Select(t => new { t.ContentId, t.Text });
            ViewBag.Topics = new SelectList(topics, "ContentId", "Text", selected);
        }

        // GET: EntrustMission/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var entrustMission = await _admin.GetAsync(id.Value);
            if (entrustMission == null) return NotFound();
            return View(entrustMission);
        }

        // POST: EntrustMission/Edit/5
        // 只能修改說明性的欄位（標題、詳情、類別、截止日期、緊急）。
        // 狀態、賞金、最佳解答、關聯內容、啟用與否牽涉預扣點數，不可在這裡修改；取消委託請用「取消委託並退款」（Cancel）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EntrustMissionId,Title,Detail,Category,Deadline,IsUrgent")] EntrustMission form)
        {
            if (id != form.EntrustMissionId) return NotFound();

            try
            {
                if (!await _admin.UpdateDetailsAsync(id, form)) return NotFound();
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["SuccessMessage"] = "委託任務已更新";
            return RedirectToAction(nameof(Index));
        }

        // POST: EntrustMission/Cancel/5
        // 取消委託並退還預扣點數給發起人（不限是否已有回覆）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _entrustMissionService.CancelEntrustMissionByAdminAsync(id);
            if (result.Ok)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: EntrustMission/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var entrustMission = await _admin.GetAsync(id.Value);
            if (entrustMission == null) return NotFound();
            return View(entrustMission);
        }

        // POST: EntrustMission/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _admin.DeleteAsync(id);
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
