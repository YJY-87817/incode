using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.ApiControllers.Chat
{
    // [ApiController]: 標記此類別為 API 控制器，啟用自動驗證與參數綁定
    [ApiController]
    // [Route]: 定義路由規則，這裡設定為 api/ChatSessionsApi
    [Route("api/ChatSessionsApi")]
    [Authorize]
    public class ChatSessionsApiController : ControllerBase
    {
        // 只能存取自己的對話 (客服 / 管理員可存取全部)
        private bool CanAccess(int ownerUserId) => User.IsInRole("Admin") || ownerUserId == User.GetId();

        // _chat: 對話與訊息的資料存取（Controller 不直接使用 DbContext）
        private readonly IChatService _chat;

        // _geminiService: 自定義的 AI 服務，用於呼叫 Google Gemini API 產生標題
        private readonly GeminiService _geminiService;

        // _hubContext: SignalR 的上下文，用於從後端主動推播訊息給前端 (例如標題更新通知)
        private readonly IHubContext<ChatHub> _hubContext;

        // 建構子 (Constructor): 透過依賴注入 (DI) 取得所需的服務實例
        private readonly ILogger<ChatSessionsApiController> _logger;

        public ChatSessionsApiController(
            IChatService chat,
            GeminiService geminiService,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatSessionsApiController> logger)
        {
            _chat = chat;
            _geminiService = geminiService;
            _hubContext = hubContext;
            _logger = logger;
        }

        // ==========================================
        // 1. [API] 取得對話列表
        // ==========================================
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetSessions(int userId)
        {
            if (!CanAccess(userId)) return NotFound();

            var sessions = await _chat.GetUserSessionsAsync(userId);

            return Ok(sessions);
        }

        // ==========================================
        // 2. [API] 建立新對話 (按 "+" 按鈕時)
        // ==========================================
        [HttpPost("Create")]
        public async Task<IActionResult> CreatChatSession([FromBody] ChatSessionDto request)
        {
            // 擁有者一律為登入者 (不採用前端傳入的 UserId)，預設為 AI 模式
            var session = await _chat.CreateSessionAsync(User.GetId(), "新對話");

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
            var session = await _chat.FindSessionAsync(sessionId);

            if (session == null || !CanAccess(session.UserId)) return NotFound("找不到對話");

            // 2. 抓取該對話的第一則 User 訊息，用來判斷話題內容；還沒有訊息就無法進行摘要
            var firstMessage = await _chat.GetFirstUserMessageAsync(sessionId);
            if (firstMessage == null) return BadRequest("還沒有訊息，無法命名");

            try
            {
                // 3. 組合 Prompt (提示詞)
                string prompt = $"請根據以下使用者的發言，幫這個聊天室取一個「非常簡短」的標題(10個字以內)，不要有引號或額外文字：\n\n{firstMessage}";

                // 呼叫 GeminiService 發送請求
                string newTitle = await _geminiService.GetGeminiReplyAsync(prompt);

                // AI 無法回覆時 GeminiService 會回傳固定的錯誤訊息，不能把它當成標題存起來
                if (newTitle == GeminiService.AiUnavailableMessage)
                    return StatusCode(503, new { message = "AI 暫時無法命名，請稍後再試" });

                // 4. 清理 AI 回傳的雜訊
                newTitle = newTitle.Replace("\"", "").Replace("「", "").Replace("」", "").Trim();
                if (newTitle.Length > 50) newTitle = newTitle.Substring(0, 50);

                // 5. 更新資料庫中的 Session 實體
                await _chat.SetTitleAsync(session, newTitle);

                // 6. 透過 SignalR 通知前端更新標題 (建議打開此註解)
                // 假設前端監聽 UpdateSessionTitle 事件
                await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("UpdateSessionTitle", sessionId, newTitle); // 只通知該對話 (原本廣播給所有連線)

                return Ok(new { sessionId, newTitle });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI 命名對話失敗 - SessionId: {SessionId}", sessionId);
                return StatusCode(500, new { message = "AI 命名失敗，請稍後再試" });
            }
        }

        // ==========================================
        // 4. [API] 刪除對話
        // ==========================================
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _chat.FindSessionAsync(id);
            if (session == null || !CanAccess(session.UserId)) return NotFound("找不到該對話");

            await _chat.DeleteSessionAsync(session);

            return Ok(new { success = true, message = "刪除成功" });
        }
    }
}