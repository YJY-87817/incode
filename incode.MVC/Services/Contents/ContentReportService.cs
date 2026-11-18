using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Contents
{
    public class ContentReportService : IContentReportService
    {
        private readonly incodedatabaseContext _context;
        private readonly ILogger<ContentReportService> _logger;

        public ContentReportService(incodedatabaseContext context, ILogger<ContentReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContentReportIndexViewModel> GetIndexAsync(int recentReviewCount = 50)
        {
            // 待處理：該文章最後一次處理（駁回 / 下架 / 恢復）之後才出現的檢舉
            var pendingGroups = await _context.ContentInteraction
                .AsNoTracking()
                .Where(r => r.CategoryId == InteractionCategoryIds.Report)
                .Where(r => !_context.ContentReportReview.Any(v => v.ContentId == r.ContentId
                    && (r.CreatedAt == null || v.CreatedAt >= r.CreatedAt)))
                .GroupBy(r => r.ContentId)
                .Select(g => new { ContentId = g.Key, Count = g.Count(), Latest = g.Max(r => r.CreatedAt) })
                .ToListAsync();

            var pending = await LoadItemsAsync(pendingGroups.Select(g => g.ContentId).ToList());
            foreach (var item in pending)
            {
                var group = pendingGroups.First(g => g.ContentId == item.ContentId);
                item.PendingReportCount = group.Count;
                item.LatestReportAt = group.Latest;
            }

            var removedIds = await _context.Content
                .Where(c => c.IsActive == ContentStatus.RemovedByAdmin)
                .Select(c => c.ContentId)
                .ToListAsync();
            var removed = await LoadItemsAsync(removedIds);

            var reviews = await _context.ContentReportReview
                .AsNoTracking()
                .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.ContentReportReviewId)
                .Take(recentReviewCount)
                .Select(v => new ContentReportReviewItem
                {
                    ContentId = v.ContentId,
                    Action = v.Action,
                    AdminName = v.AdminUser.Username,
                    Note = v.Note,
                    CreatedAt = v.CreatedAt,
                })
                .ToListAsync();

            // 回覆類的標題是空的：與清單相同，顯示所屬主題的標題
            var reviewTopics = (await LoadItemsAsync(reviews.Select(r => r.ContentId).Distinct().ToList()))
                .ToDictionary(i => i.ContentId, i => i.TopicTitle);
            foreach (var review in reviews)
                review.TopicTitle = reviewTopics.GetValueOrDefault(review.ContentId);

            return new ContentReportIndexViewModel
            {
                Pending = pending.OrderByDescending(i => i.PendingReportCount).ThenByDescending(i => i.LatestReportAt).ToList(),
                Removed = removed.OrderByDescending(i => i.ContentId).ToList(),
                RecentReviews = reviews,
            };
        }

        // 內容的顯示資料；回覆類以所屬主題的標題顯示（最多往上找 3 層：留言回覆 → 留言 → 回文 → 主題）
        private async Task<List<ReportedContentItem>> LoadItemsAsync(List<int> contentIds)
        {
            if (contentIds.Count == 0) return new();

            var rows = await _context.Content
                .AsNoTracking()
                .Where(c => contentIds.Contains(c.ContentId))
                .Select(c => new
                {
                    c.ContentId, c.Type, c.Title, c.IsActive, c.CreatedAt, c.ParentContentId,
                    AuthorName = c.User.Username,
                })
                .ToListAsync();

            // 一層一層往上找（每層一次查詢）：key = 原本的內容，value = 目前找到的祖先
            var topics = rows.ToDictionary(r => r.ContentId, r => (Id: (int?)r.ContentId, r.Title, r.ParentContentId));
            for (int depth = 0; depth < 3; depth++)
            {
                var parentIds = topics.Values.Where(t => t.ParentContentId != null).Select(t => t.ParentContentId!.Value).Distinct().ToList();
                if (parentIds.Count == 0) break;
                var parents = await _context.Content.AsNoTracking()
                    .Where(c => parentIds.Contains(c.ContentId))
                    .Select(c => new { c.ContentId, c.Title, c.ParentContentId })
                    .ToDictionaryAsync(c => c.ContentId);
                foreach (var key in topics.Keys.ToList())
                {
                    var current = topics[key];
                    if (current.ParentContentId is int pid && parents.TryGetValue(pid, out var parent))
                        topics[key] = (parent.ContentId, parent.Title, parent.ParentContentId);
                    else if (current.ParentContentId != null)
                        topics[key] = (current.Id, current.Title, null); // 父文章不存在：停在這一層
                }
            }

            return rows.Select(r => new ReportedContentItem
            {
                ContentId = r.ContentId,
                Type = r.Type,
                TopicId = topics[r.ContentId].Id,
                TopicTitle = topics[r.ContentId].Title,
                AuthorName = r.AuthorName,
                Status = r.IsActive,
                CreatedAt = r.CreatedAt,
            }).ToList();
        }

        public async Task DismissAsync(int contentId, int adminUserId, string? note)
        {
            if (!await _context.Content.AnyAsync(c => c.ContentId == contentId))
                throw new BusinessException("找不到這篇內容");

            AddReview(contentId, adminUserId, ContentReportActions.Dismissed, note);
            await _context.SaveChangesAsync();
            _logger.LogInformation("管理員 {AdminUserId} 駁回內容 {ContentId} 的檢舉", adminUserId, contentId);
        }

        public Task RemoveAsync(int contentId, int adminUserId, string? note)
            => ChangeStatusAsync(contentId, adminUserId, note,
                from: ContentStatus.Active, to: ContentStatus.RemovedByAdmin, ContentReportActions.Removed);

        public Task RestoreAsync(int contentId, int adminUserId, string? note)
            => ChangeStatusAsync(contentId, adminUserId, note,
                from: ContentStatus.RemovedByAdmin, to: ContentStatus.Active, ContentReportActions.Restored);

        private async Task ChangeStatusAsync(int contentId, int adminUserId, string? note, int from, int to, string action)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // 條件更新：同時有兩位管理員操作，或作者剛好刪除時，只有狀態相符的那一次會成功
            var updated = await _context.Content
                .Where(c => c.ContentId == contentId && c.IsActive == from)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, to));

            if (updated == 0)
            {
                var status = await _context.Content.Where(c => c.ContentId == contentId)
                    .Select(c => (int?)c.IsActive).FirstOrDefaultAsync();
                throw new BusinessException(status switch
                {
                    null => "找不到這篇內容",
                    ContentStatus.DeletedByAuthor => "作者已刪除這篇內容",
                    ContentStatus.RemovedByAdmin => "這篇內容已經下架",
                    ContentStatus.Active => "這篇內容目前是公開的",
                    _ => "內容狀態不符，無法處理",
                });
            }

            AddReview(contentId, adminUserId, action, note);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            _logger.LogInformation("管理員 {AdminUserId} 將內容 {ContentId} {Action}", adminUserId, contentId, action);
        }

        private void AddReview(int contentId, int adminUserId, string action, string? note)
        {
            note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (note != null && note.Length > ContentReportActions.MaxNoteLength)
                throw new BusinessException($"備註最多 {ContentReportActions.MaxNoteLength} 字");

            _context.ContentReportReview.Add(new ContentReportReview
            {
                ContentId = contentId,
                AdminUserId = adminUserId,
                Action = action,
                Note = note,
                CreatedAt = DateTime.Now,
            });
        }
    }
}
