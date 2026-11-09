using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly incodedatabaseContext _context;

        public ChatService(incodedatabaseContext context)
        {
            _context = context;
        }

        public Task<bool> IsOwnerAsync(int sessionId, int userId) =>
            _context.ChatSession.AnyAsync(s => s.ChatSessionId == sessionId && s.UserId == userId);

        public async Task<ChatSession?> FindSessionAsync(int sessionId) =>
            await _context.ChatSession.FindAsync(sessionId);

        public async Task<(ChatSession? Session, bool IsNew)> GetOrInitSessionAsync(int userId, int requestedSessionId, string initialTitle = "我的對話")
        {
            ChatSession? session = null;

            // 前端有指定對話：只有擁有者能用（不是自己的視為沒權限，不另開新對話）
            if (requestedSessionId > 0)
            {
                session = await _context.ChatSession.FindAsync(requestedSessionId);
                if (session != null && session.UserId != userId) return (null, false);
            }

            // 沒指定或找不到：接續該會員最新的對話
            session ??= await _context.ChatSession
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (session == null)
            {
                session = await CreateSessionAsync(userId, initialTitle);
                return (session, true);
            }

            session.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return (session, false);
        }

        public async Task<ChatSession> CreateSessionAsync(int userId, string title)
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = title,
                Ishumanmode = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ChatSession.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task AddMessageAsync(int sessionId, string role, string body)
        {
            _context.ChatMessage.Add(new ChatMessage
            {
                SessionId = sessionId,
                Role = role,
                Body = body,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task SetHumanModeAsync(ChatSession session, bool isHumanMode)
        {
            session.Ishumanmode = isHumanMode;
            session.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public Task<List<SupportSessionDto>> GetSupportSessionsAsync() =>
            _context.ChatSession
                .Where(s => s.Ishumanmode == true)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new SupportSessionDto
                {
                    ChatSessionId = s.ChatSessionId,
                    UserId = s.UserId,
                    Title = s.Title,
                    LastUpdate = s.UpdatedAt.HasValue ? s.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    // 最後一則訊息當預覽，讓客服知道客戶在問什麼
                    LastMessage = _context.ChatMessage
                        .Where(m => m.SessionId == s.ChatSessionId)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Body)
                        .FirstOrDefault()
                })
                .ToListAsync();

        public Task<List<ChatSessionDto>> GetUserSessionsAsync(int userId) =>
            _context.ChatSession
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new ChatSessionDto
                {
                    ChatSessionId = s.ChatSessionId,
                    UserId = s.UserId,
                    Title = s.Title,
                    Ishumanmode = s.Ishumanmode,
                    CreatedAt = s.CreatedAt ?? DateTime.MinValue
                })
                .ToListAsync();

        public Task<List<ChatMessageDto>> GetHistoryAsync(int sessionId) =>
            _context.ChatMessage
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    ChatMessageId = m.ChatMessageId,
                    SessionId = m.SessionId,
                    Role = m.Role,
                    Body = m.Body,
                    CreatedAt = m.CreatedAt.HasValue ? m.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
                })
                .ToListAsync();

        public Task<string?> GetFirstUserMessageAsync(int sessionId) =>
            _context.ChatMessage
                .Where(m => m.SessionId == sessionId && m.Role == "user")
                .OrderBy(m => m.CreatedAt)
                .Select(m => m.Body)
                .FirstOrDefaultAsync();

        public async Task SetTitleAsync(ChatSession session, string title)
        {
            session.Title = title;
            session.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(ChatSession session)
        {
            _context.ChatMessage.RemoveRange(_context.ChatMessage.Where(m => m.SessionId == session.ChatSessionId));
            _context.ChatSession.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}
