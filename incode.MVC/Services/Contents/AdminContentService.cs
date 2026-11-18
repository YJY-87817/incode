using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
    public class AdminContentService : IAdminContentService
    {
        private readonly incodedatabaseContext _context;

        public AdminContentService(incodedatabaseContext context)
        {
            _context = context;
        }

        public Task<List<ContentViewModel>> GetContentsAsync()
            => _context.Content
                .AsNoTracking()
                .OrderBy(c => c.ContentId)
                .Select(c => new ContentViewModel
                {
                    ContentId = c.ContentId,
                    ParentContentId = c.ParentContentId,
                    Title = c.Title,
                    Body = c.Body.Substring(0, Math.Min(100, c.Body.Length)), // 只取前 100 字
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    AuthorName = c.User.Username,
                    CategoryName = c.Category != null ? c.Category.CategoryName : null,
                })
                .ToListAsync();

        public async Task<ContentDetailViewModel?> GetContentDetailAsync(int contentId)
        {
            var content = await _context.Content
                .AsNoTracking()
                .Where(c => c.ContentId == contentId)
                .Select(c => new ContentDetailViewModel
                {
                    ContentId = c.ContentId,
                    Title = c.Title,
                    Body = c.Body,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                    AuthorName = c.User.Username,
                    // 沒有分類的內容（舊資料）原本在這裡 NullReferenceException → 500
                    CategoryName = c.Category != null ? c.Category.CategoryName : null,
                    ParentContentId = c.ParentContentId,
                    Type = c.Type,
                    Status = c.IsActive,
                })
                .FirstOrDefaultAsync();
            if (content == null) return null;

            content.Replies = await _context.Content
                .AsNoTracking()
                .Where(c => c.ParentContentId == contentId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ReplyViewModel
                {
                    ContentId = c.ContentId,
                    Body = c.Body,
                    AuthorName = c.User.Username,
                    CreatedAt = c.CreatedAt ?? DateTime.MinValue,
                })
                .ToListAsync();
            return content;
        }

        public Task<List<ContentEditViewModel>> GetLatestEditsAsync()
            => _context.Content
                .AsNoTracking()
                .Select(d => new
                {
                    Content = d,
                    Latest = d.ContentHistory.OrderByDescending(ch => ch.CreatedAt).FirstOrDefault(),
                })
                .Select(x => new ContentEditViewModel
                {
                    ContentId = x.Content.ContentId,
                    ParentContentId = x.Content.ParentContentId,
                    CategoryId = x.Content.CategoryId,
                    UserId = x.Content.UserId,
                    Title = x.Content.Title,
                    Body = x.Content.Body,
                    CreatedAt = x.Content.CreatedAt,
                    UpdatedAt = x.Content.UpdatedAt,
                    ContentHistoryId = x.Latest != null ? x.Latest.ContentHistoryId : 0,
                    OldTitle = x.Latest != null ? x.Latest.OldTitle : string.Empty,
                    OldBody = x.Latest != null ? x.Latest.OldBody : string.Empty,
                })
                .ToListAsync();

        public Task<List<ContentHistory>> GetHistoriesAsync()
            => _context.ContentHistory.Include(c => c.User).AsNoTracking().ToListAsync();

        public Task<ContentHistory?> GetHistoryAsync(int contentHistoryId)
            => _context.ContentHistory
                .Include(c => c.Content)
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ContentHistoryId == contentHistoryId);
    }
}
