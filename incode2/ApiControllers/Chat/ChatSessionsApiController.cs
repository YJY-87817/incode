using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.ApiControllers.Chat
{
    // [ApiController]: 標記此類別為 API 控制器，啟用自動驗證與參數綁定
    [ApiController]
    // [Route]: 定義路由規則，這裡設定為 api/ChatSessionsApi
    [Route("api/ChatSessionsApi")]
    public class ChatSessionsApiController : ControllerBase
    {
        // _context: 資料庫上下文，用於與 SQL Server 進行 CRUD 操作
        private readonly incodedatabaseContext _context;

        // _geminiService: 自定義的 AI 服務，用於呼叫 Google Gemini API 產生標題
        private readonly GeminiService _geminiService;

        // _hubContext: SignalR 的上下文，用於從後端主動推播訊息給前端 (例如標題更新通知)
        private readonly IHubContext<ChatHub> _hubContext;

        // 建構子 (Constructor): 透過依賴注入 (DI) 取得所需的服務實例
        public ChatSessionsApiController(
            incodedatabaseContext context,
            GeminiService geminiService,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _geminiService = geminiService;
            _hubContext = hubContext;
        }

        // ==========================================
        // 1. [API] 取得對話列表
        // ==========================================
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetSessions(int userId)
        {
            // 使用 EF Core 查詢 ChatSession 資料表
            var sessions = await _context.ChatSession
                .Where(s => s.UserId == userId) // 過濾條件：只抓取該使用者的對話
                .OrderByDescending(s => s.UpdatedAt) // 排序：依照更新時間由新到舊
                .Select(s => new ChatSessionDto // 投影為 DTO
                {
                    ChatSessionId = s.ChatSessionId,
                    UserId = s.UserId,
                    Title = s.Title,
                    Ishumanmode = s.Ishumanmode, // 回傳目前模式 (前端列表需顯示)
                    CreatedAt = s.CreatedAt ?? DateTime.MinValue // 如果 s.CreatedAt 是 null，就給它 DateTime.MinValue (或 DateTime.Now)
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // ==========================================
        // 2. [API] 建立新對話 (按 "+" 按鈕時)
        // ==========================================
        [HttpPost("Create")]
        public async Task<IActionResult> CreatChatSession([FromBody] ChatSessionDto request)
        {
            // 建立一個新的 ChatSession 實體物件
            var session = new ChatSession
            {
                UserId = request.UserId, // 設定擁有者 ID
                Title = "新對話",        // 預設標題
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Ishumanmode = false // ★ 預設為 AI 模式
            };

            _context.ChatSession.Add(session);
            await _context.SaveChangesAsync();

            // 回傳 HTTP 200 OK，並包含新生成的 ID 與標題
            return Ok(new { sessionId = session.ChatSessionId, title = session.Title, isHumanMode = false });
        }

        // ==========================================
        // 3. [API] ★ AI 自動產生標題 (進階功能)
        // ==========================================
        [HttpPost("AutoTitle/{sessionId}")]
        public async Task<IActionResult> AutoGenerateTitle(int sessionId)
        {
            // 1. 先從資料庫找出這個對話 Session
            var session = await _context.ChatSession.FindAsync(sessionId);

            if (session == null) return NotFound("找不到對話");

            // 2. 抓取該對話的第一則 User 訊息，用來判斷話題內容
            var firstMessage = await _context.ChatMessage
                .Where(m => m.SessionId == sessionId && m.Role == "user")
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            // 如果還沒有訊息，無法進行摘要
            if (firstMessage == null) return BadRequest("還沒有訊息，無法命名");

            try
            {
                // 3. 組合 Prompt (提示詞)
                string prompt = $"請根據以下使用者的發言，幫這個聊天室取一個「非常簡短」的標題(10個字以內)，不要有引號或額外文字：\n\n{firstMessage.Body}";

                // 呼叫 GeminiService 發送請求
                string newTitle = await _geminiService.GetGeminiReplyAsync(prompt);

                // 4. 清理 AI 回傳的雜訊
                newTitle = newTitle.Replace("\"", "").Replace("「", "").Replace("」", "").Trim();
                if (newTitle.Length > 50) newTitle = newTitle.Substring(0, 50);

                // 5. 更新資料庫中的 Session 實體
                session.Title = newTitle;
                session.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // 6. 透過 SignalR 通知前端更新標題 (建議打開此註解)
                // 假設前端監聽 UpdateSessionTitle 事件
                await _hubContext.Clients.All.SendAsync("UpdateSessionTitle", sessionId, newTitle);

                return Ok(new { sessionId, newTitle });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"AI 命名失敗: {ex.Message}");
            }
        }

        // ==========================================
        // 4. [API] 刪除對話
        // ==========================================
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.ChatSession.FindAsync(id);
            if (session == null) return NotFound("找不到該對話");

            // 1. 刪除關聯的訊息
            var messages = _context.ChatMessage.Where(m => m.SessionId == id);
            _context.ChatMessage.RemoveRange(messages);

            // 2. 刪除關聯的圖片記錄 (建議也要刪除 UserImage 表中的關聯，如果有的話)
            // var images = _context.UserImage.Where(...);

            // 3. 刪除對話本身
            _context.ChatSession.Remove(session);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "刪除成功" });
        }
    }
}