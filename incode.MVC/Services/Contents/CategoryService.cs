using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
    public class CategoryService : ICategoryService
    {
        private readonly incodedatabaseContext _context;

        public CategoryService(incodedatabaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 不能使用的分類：停用（<see cref="ContentCategoryStatus.Disabled"/>）的分類，以及上層任何一層被停用的子分類。
        /// 前台分類清單不列出、不能發新主題、不能在底下新增子分類（第三次全專案檢查 48）。
        /// 分類資料表很小，一次讀出後在記憶體計算（父分類形成循環的舊資料也不會無限迴圈）
        /// </summary>
        public static async Task<HashSet<int>> GetUnavailableCategoryIdsAsync(incodedatabaseContext context)
        {
            var rows = await context.ContentCategory
                .AsNoTracking()
                .Select(c => new { c.ContentCategoryId, c.ParentCategoryId, c.IsActive })
                .ToListAsync();
            var byId = rows.ToDictionary(r => r.ContentCategoryId);

            var unavailable = new HashSet<int>();
            foreach (var row in rows)
            {
                var visited = new HashSet<int>();
                for (var current = row; current != null && visited.Add(current.ContentCategoryId);
                     current = current.ParentCategoryId is int parentId && byId.TryGetValue(parentId, out var parent) ? parent : null)
                {
                    if (current.IsActive == ContentCategoryStatus.Disabled)
                    {
                        unavailable.Add(row.ContentCategoryId);
                        break;
                    }
                }
            }
            return unavailable;
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            var unavailable = await GetUnavailableCategoryIdsAsync(_context);

            // 1) 分類（不含統計數）；停用的分類與其子分類不列出
            var categories = (await _context.ContentCategory
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    Id = c.ContentCategoryId,
                    ParentCategoryId = c.ParentCategoryId,
                    Name = c.CategoryName,
                    Description = c.Description,
                    TopicCount = 0,
                    PostCount = 0
                })
                .ToListAsync())
                .Where(c => !unavailable.Contains(c.Id))
                .ToList();

            // 2) 從 content 表計算每個分類的主題數 / 文章數：只算公開中的（IsActive = 1）。
            //    刪除是 2（ContentService.DeleteContent），原本排除的是 3，已刪除的文章也被算進去
            var counts = await _context.Content
                .AsNoTracking()
                .Where(c => c.IsActive == 1 && c.CategoryId != null)
                .GroupBy(c => c.CategoryId!.Value)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    PostCount = g.Count(),
                    TopicCount = g.Count(c => c.ParentContentId == null)
                })
                .ToDictionaryAsync(x => x.CategoryId);

            foreach (var cat in categories)
            {
                if (counts.TryGetValue(cat.Id, out var c))
                {
                    cat.PostCount = c.PostCount;
                    cat.TopicCount = c.TopicCount;
                }
            }

            return categories.OrderBy(c => c.Id).ToList();
        }

        public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto request)
        {
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.ContentCategory
                    .AsNoTracking()
                    .AnyAsync(c => c.ContentCategoryId == request.ParentCategoryId.Value);
                // 停用（或在停用分類底下）的父分類視同不存在
                if (!parentExists || (await GetUnavailableCategoryIdsAsync(_context)).Contains(request.ParentCategoryId.Value))
                    return null;
            }

            var entity = new ContentCategory
            {
                ParentCategoryId = request.ParentCategoryId,
                CategoryName = request.Name,
                Description = request.Description,
                IsActive = 1
            };
            _context.ContentCategory.Add(entity);
            await _context.SaveChangesAsync();

            return new CategoryDto
            {
                Id = entity.ContentCategoryId,
                ParentCategoryId = entity.ParentCategoryId,
                Name = entity.CategoryName,
                Description = entity.Description,
                TopicCount = 0,
                PostCount = 0
            };
        }
    }
}
