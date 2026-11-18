using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Chat
{
    public class AdminChatService : IAdminChatService
    {
        private readonly incodedatabaseContext _context;

        public AdminChatService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<ChatIndexViewModel> GetIndexAsync(int? sessionId)
        {
            var model = new ChatIndexViewModel
            {
                Sessions = await _context.ChatSession.AsNoTracking()
                    .Include(s => s.User)
                    .OrderByDescending(s => s.UpdatedAt)
                    .ToListAsync(),
                CurrentMessages = new List<ChatMessage>()
            };

            // 沒指定就顯示最新的一筆
            int? targetId = sessionId ?? model.Sessions.FirstOrDefault()?.ChatSessionId;
            if (!targetId.HasValue) return model;

            model.CurrentChatSessionId = targetId;
            var current = model.Sessions.FirstOrDefault(s => s.ChatSessionId == targetId);
            model.TargetUser = current?.User;
            model.IsHumanMode = current?.Ishumanmode ?? false;

            model.CurrentMessages = await _context.ChatMessage.AsNoTracking()
                .Where(m => m.SessionId == targetId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            return model;
        }

        public Task<List<ChatSession>> GetSessionsAsync() =>
            _context.ChatSession.AsNoTracking()
                .Include(c => c.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public Task<ChatSession?> GetSessionAsync(int sessionId) =>
            _context.ChatSession.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(s => s.ChatSessionId == sessionId);
    }
}
