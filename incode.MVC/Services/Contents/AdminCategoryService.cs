using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
    public class AdminCategoryService : IAdminCategoryService
    {
        private readonly incodedatabaseContext _context;

        public AdminCategoryService(incodedatabaseContext context)
        {
            _context = context;
        }

        public static string StatusName(int status) => status switch
        {
            ContentCategoryStatus.MemberCreated => "會員新增",
            ContentCategoryStatus.Official => "官方",
            ContentCategoryStatus.Disabled => "停用",
            _ => $"未知（{status}）",
        };

        public static readonly int[] Statuses = { ContentCategoryStatus.Official, ContentCategoryStatus.MemberCreated, ContentCategoryStatus.Disabled };

        public async Task<List<AdminCategoryRow>> GetRowsAsync()
        {
            var rows = await _context.ContentCategory
                .AsNoTracking()
                .Select(c => new { c.ContentCategoryId, c.ParentCategoryId, c.CategoryName, c.Description, c.IsActive })
                .ToListAsync();
            return rows.Select(c => new AdminCategoryRow(c.ContentCategoryId, c.ParentCategoryId, c.CategoryName, c.Description,
                c.IsActive, StatusName(c.IsActive))).ToList();
        }

        public Task<ContentCategory?> GetAsync(int contentCategoryId)
            => _context.ContentCategory.Include(c => c.ParentCategory).AsNoTracking()
                .FirstOrDefaultAsync(m => m.ContentCategoryId == contentCategoryId);

        public async Task<List<(int Id, string Name)>> GetParentOptionsAsync(int? editingId = null)
        {
            var all = await _context.ContentCategory.AsNoTracking()
                .Select(c => new { c.ContentCategoryId, c.ParentCategoryId, c.CategoryName }).ToListAsync();
            var excluded = editingId.HasValue ? DescendantsAndSelf(editingId.Value, all.Select(c => (c.ContentCategoryId, c.ParentCategoryId))) : new HashSet<int>();
            return all.Where(c => !excluded.Contains(c.ContentCategoryId))
                .Select(c => (c.ContentCategoryId, c.CategoryName)).ToList();
        }

        private static HashSet<int> DescendantsAndSelf(int id, IEnumerable<(int Id, int? ParentId)> all)
        {
            var list = all.ToList();
            var result = new HashSet<int> { id };
            bool added = true;
            while (added)
            {
                added = false;
                foreach (var c in list)
                    if (c.ParentId is int p && result.Contains(p) && result.Add(c.Id)) added = true;
            }
            return result;
        }

        public async Task<ContentCategory> CreateAsync(ContentCategory form)
        {
            var category = new ContentCategory();
            await ApplyAsync(category, form, editingId: null);
            _context.ContentCategory.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateAsync(int contentCategoryId, ContentCategory form)
        {
            var category = await _context.ContentCategory.FirstOrDefaultAsync(c => c.ContentCategoryId == contentCategoryId);
            if (category == null) return false;

            await ApplyAsync(category, form, contentCategoryId);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ApplyAsync(ContentCategory category, ContentCategory form, int? editingId)
        {
            if (string.IsNullOrWhiteSpace(form.CategoryName))
                throw new BusinessException("請填寫分類名稱");
            // 原本表單沒有狀態欄位，存檔時一律變成 0（停用的分類編輯後就被重新啟用）
            if (!Statuses.Contains(form.IsActive))
                throw new BusinessException("請選擇分類狀態");

            if (form.ParentCategoryId is int parentId)
            {
                var all = await _context.ContentCategory.AsNoTracking()
                    .Select(c => new { c.ContentCategoryId, c.ParentCategoryId }).ToListAsync();
                if (!all.Any(c => c.ContentCategoryId == parentId))
                    throw new BusinessException("父分類不存在");
                // 不能把自己或自己的子孫設成父分類（會形成循環）
                if (editingId.HasValue && DescendantsAndSelf(editingId.Value, all.Select(c => (c.ContentCategoryId, c.ParentCategoryId))).Contains(parentId))
                    throw new BusinessException("不能把自己或自己的子分類設為父分類");
            }

            category.CategoryName = form.CategoryName.Trim();
            category.Description = string.IsNullOrWhiteSpace(form.Description) ? null! : form.Description.Trim();
            category.ParentCategoryId = form.ParentCategoryId;
            category.IsActive = form.IsActive;
        }

        public async Task DeleteAsync(int contentCategoryId)
        {
            var category = await _context.ContentCategory.FirstOrDefaultAsync(c => c.ContentCategoryId == contentCategoryId);
            if (category == null) return;

            // 原本直接刪除：還有文章或子分類時撞外鍵 → 500
            if (await _context.Content.AnyAsync(c => c.CategoryId == contentCategoryId))
                throw new BusinessException("這個分類還有文章，不能刪除；請改為「停用」");
            if (await _context.ContentCategory.AnyAsync(c => c.ParentCategoryId == contentCategoryId))
                throw new BusinessException("這個分類還有子分類，請先移動或刪除子分類");

            _context.ContentCategory.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
