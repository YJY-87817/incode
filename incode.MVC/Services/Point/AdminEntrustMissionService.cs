using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Point
{
    public class AdminEntrustMissionService : IAdminEntrustMissionService
    {
        private readonly incodedatabaseContext _context;
        private readonly IEntrustMissionService _entrustMissionService;

        public AdminEntrustMissionService(incodedatabaseContext context, IEntrustMissionService entrustMissionService)
        {
            _context = context;
            _entrustMissionService = entrustMissionService;
        }

        private IQueryable<EntrustMission> WithRelations()
            => _context.EntrustMission
                .Include(e => e.BestContent)
                .Include(e => e.BestUser)
                .Include(e => e.Content)
                .Include(e => e.User)
                .AsNoTracking();

        public Task<List<EntrustMission>> GetAllAsync() => WithRelations().ToListAsync();

        public Task<EntrustMission?> GetAsync(int entrustMissionId)
            => WithRelations().FirstOrDefaultAsync(e => e.EntrustMissionId == entrustMissionId);

        public async Task<List<(int ContentId, string Text)>> GetEligibleTopicsAsync()
        {
            var topics = await _context.Content
                .AsNoTracking()
                .Where(c => c.IsActive == ContentStatus.Active && c.Type == ContentTypes.Post && c.ParentContentId == null)
                .Where(c => !_context.EntrustMission.Any(em => em.ContentId == c.ContentId && em.IsActive
                    && em.Status != "cancelled" && em.Status != "expired"))
                .OrderByDescending(c => c.ContentId)
                .Select(c => new { c.ContentId, c.Title, Author = c.User.Username })
                .ToListAsync();
            return topics.Select(t => (t.ContentId, $"#{t.ContentId} {t.Title}（{t.Author}）")).ToList();
        }

        public async Task<int> CreateAsync(EntrustMissionCreateViewModel form)
        {
            // 委託人一律是文章作者（委託只能掛在自己的主題上），賞金從作者的點數預扣
            var authorId = await _context.Content.Where(c => c.ContentId == form.ContentId)
                .Select(c => (int?)c.UserId).FirstOrDefaultAsync()
                ?? throw new BusinessException("找不到這篇文章");

            var result = await _entrustMissionService.CreateEntrustMissionAsync(new EntrustMissionDTO
            {
                UserId = authorId,
                ContentId = form.ContentId,
                Title = form.Title.Trim(),
                Detail = form.Detail.Trim(),
                RewardPoints = form.RewardPoints,
                Category = form.Category.Trim(),
                Deadline = form.Deadline,
                IsUrgent = form.IsUrgent,
            });
            if (!result.Ok)
                throw new BusinessException(result.Message ?? "建立委託失敗");

            return await _context.EntrustMission.AsNoTracking()
                .Where(e => e.ContentId == form.ContentId && e.Status == "open")
                .OrderByDescending(e => e.EntrustMissionId)
                .Select(e => e.EntrustMissionId)
                .FirstAsync();
        }

        public async Task<bool> UpdateDetailsAsync(int entrustMissionId, EntrustMission form)
        {
            var entrustMission = await _context.EntrustMission.FirstOrDefaultAsync(e => e.EntrustMissionId == entrustMissionId);
            if (entrustMission == null) return false;

            if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.Category))
                throw new BusinessException("任務標題與類別為必填");

            // 狀態、賞金、最佳解答、關聯內容、啟用與否牽涉預扣點數，不能在這裡修改
            entrustMission.Title = form.Title.Trim();
            entrustMission.Detail = form.Detail;
            entrustMission.Category = form.Category.Trim();
            entrustMission.Deadline = form.Deadline;
            entrustMission.IsUrgent = form.IsUrgent;
            entrustMission.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAsync(int entrustMissionId)
        {
            var entrustMission = await _context.EntrustMission.FirstOrDefaultAsync(e => e.EntrustMissionId == entrustMissionId);
            if (entrustMission == null) return;

            // 徵求中的委託還有預扣點數，直接刪除不會退點：先取消
            if (entrustMission.Status == "open")
                throw new BusinessException("徵求中的委託請先「取消委託並退款」再刪除");

            _context.EntrustMission.Remove(entrustMission);
            await _context.SaveChangesAsync();
        }
    }
}
