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

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            // 1) 分類（不含統計數）
            var categories = await _context.ContentCategory
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
                .ToListAsync();

            // 2) 從 content 表計算每個分類的主題數 / 文章數（IsActive = 3 為已刪除，不計）
            var counts = await _context.Content
                .AsNoTracking()
                .Where(c => c.IsActive != 3 && c.CategoryId != null)
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
                if (!parentExists) return null;
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
