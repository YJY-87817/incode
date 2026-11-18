using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace incode.MVC.Controllers.Contents
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ContentCategoryController : Controller
    {
        private readonly IAdminCategoryService _categories;

        public ContentCategoryController(IAdminCategoryService categories)
        {
            _categories = categories;
        }

        // GET: ContentCategory（資料由 DataTables 呼叫 IndexJson 取得）
        public IActionResult Index()
        {
            return View();
        }

        // GET: ContentCategory/IndexJson（唯讀資料，DataTables 以 GET 取得）
        [HttpGet]
        public async Task<IActionResult> IndexJson()
        {
            return Json(await _categories.GetRowsAsync());
        }

        // GET: ContentCategory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var contentCategory = await _categories.GetAsync(id.Value);
            if (contentCategory == null) return NotFound();
            return View(contentCategory);
        }

        // GET: ContentCategory/Create
        public async Task<IActionResult> Create()
        {
            await LoadOptionsAsync(null, null, null);
            return View(new ContentCategory { IsActive = ContentCategoryStatus.Official });
        }

        // POST: ContentCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ParentCategoryId,CategoryName,Description,IsActive")] ContentCategory contentCategory)
        {
            try
            {
                await _categories.CreateAsync(contentCategory);
                TempData["SuccessMessage"] = "分類已新增";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadOptionsAsync(null, contentCategory.ParentCategoryId, contentCategory.IsActive);
                return View(contentCategory);
            }
        }

        // GET: ContentCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var contentCategory = await _categories.GetAsync(id.Value);
            if (contentCategory == null) return NotFound();
            await LoadOptionsAsync(id, contentCategory.ParentCategoryId, contentCategory.IsActive);
            return View(contentCategory);
        }

        // POST: ContentCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContentCategoryId,ParentCategoryId,CategoryName,Description,IsActive")] ContentCategory contentCategory)
        {
            if (id != contentCategory.ContentCategoryId) return NotFound();

            try
            {
                if (!await _categories.UpdateAsync(id, contentCategory)) return NotFound();
                TempData["SuccessMessage"] = "分類已更新";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadOptionsAsync(id, contentCategory.ParentCategoryId, contentCategory.IsActive);
                return View(contentCategory);
            }
        }

        // 父分類（編輯時排除自己與子孫，避免形成循環）與狀態下拉選單。
        // 目前的狀態不在選單內（例如舊後台建立的 0）時加進選單並選取，存檔時才不會被默默改成第一個選項
        private async Task LoadOptionsAsync(int? editingId, int? selectedParentId, int? currentStatus)
        {
            var parents = (await _categories.GetParentOptionsAsync(editingId)).Select(p => new { p.Id, p.Name });
            ViewData["ParentCategoryId"] = new SelectList(parents, "Id", "Name", selectedParentId);
            var statuses = ContentCategoryStatus.Selectable.ToList();
            if (currentStatus is int current && !statuses.Contains(current)) statuses.Add(current);
            ViewData["Statuses"] = statuses
                .Select(s => new SelectListItem(ContentCategoryStatus.DisplayName(s), s.ToString())).ToList();
        }

        // GET: ContentCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var contentCategory = await _categories.GetAsync(id.Value);
            if (contentCategory == null) return NotFound();
            return View(contentCategory);
        }

        // POST: ContentCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categories.DeleteAsync(id);
                TempData["SuccessMessage"] = "分類已刪除";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
