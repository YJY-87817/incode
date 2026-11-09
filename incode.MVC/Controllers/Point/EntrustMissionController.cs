using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Controllers.Point
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class EntrustMissionController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IEntrustMissionService _entrustMissionService;

        public EntrustMissionController(incodedatabaseContext context, IEntrustMissionService entrustMissionService)
        {
            _context = context;
            _entrustMissionService = entrustMissionService;
        }

        // GET: EntrustMissionTest
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.EntrustMission.Include(e => e.BestContent).Include(e => e.BestUser).Include(e => e.Content).Include(e => e.User);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: EntrustMissionTest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }

            return View(entrustMission);
        }

        // GET: EntrustMissionTest/Create
        public IActionResult Create()
        {
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body");
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email");
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body");
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email");
            return View();
        }

        // POST: EntrustMissionTest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EntrustMissionId,UserId,ContentId,Title,Detail,RewardPoints,Category,Deadline,Status,IsUrgent,IsActive,CreatedAt,UpdatedAt,BestContentId,BestUserId,SelectedAt")] EntrustMission entrustMission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entrustMission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.BestContentId);
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.BestUserId);
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.ContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.UserId);
            return View(entrustMission);
        }

        // GET: EntrustMissionTest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }
            ViewData["BestContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.BestContentId);
            ViewData["BestUserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.BestUserId);
            ViewData["ContentId"] = new SelectList(_context.Content, "ContentId", "Body", entrustMission.ContentId);
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Email", entrustMission.UserId);
            return View(entrustMission);
        }

        // POST: EntrustMissionTest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 只能修改說明性的欄位（標題、詳情、類別、截止日期、緊急）。
        // 狀態、賞金、最佳解答、關聯內容、啟用與否牽涉預扣點數，原本可直接改（例如改成已取消卻不退點），
        // 現在不可在這裡修改；取消委託請用下方的「取消委託並退款」（Cancel）
        public async Task<IActionResult> Edit(int id, [Bind("EntrustMissionId,Title,Detail,Category,Deadline,IsUrgent")] EntrustMission form)
        {
            if (id != form.EntrustMissionId)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.Category))
            {
                TempData["ErrorMessage"] = "任務標題與類別為必填";
                return RedirectToAction(nameof(Edit), new { id });
            }

            entrustMission.Title = form.Title.Trim();
            entrustMission.Detail = form.Detail;
            entrustMission.Category = form.Category.Trim();
            entrustMission.Deadline = form.Deadline;
            entrustMission.IsUrgent = form.IsUrgent;
            entrustMission.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

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

        // GET: EntrustMissionTest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entrustMission = await _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EntrustMissionId == id);
            if (entrustMission == null)
            {
                return NotFound();
            }

            return View(entrustMission);
        }

        // POST: EntrustMissionTest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entrustMission = await _context.EntrustMission.FindAsync(id);
            if (entrustMission != null)
            {
                // 徵求中的委託還有預扣點數，直接刪除不會退點：先取消
                if (entrustMission.Status == "open")
                {
                    TempData["ErrorMessage"] = "徵求中的委託請先「取消委託並退款」再刪除";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                _context.EntrustMission.Remove(entrustMission);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EntrustMissionExists(int id)
        {
            return _context.EntrustMission.Any(e => e.EntrustMissionId == id);
        }
    }
}
