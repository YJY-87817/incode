using incode2.Models;
using incode2.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using incode2.Dtos.Chat;
using incode2.Services.Chat;

namespace incode2.ApiControllers.Chat
{
    // [ApiController]: 標記此類別為 API 控制器，啟用自動驗證與參數綁定
    [ApiController]
    // [Route]: 定義路由規則，[controller] 會自動替換為 ChatSessionsApi
    [Route("api/[controller]")] // 實際網址路徑: /api/ChatSessionsApi
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
            incodedatabaseContext context, // 注入資料庫上下文
            GeminiService geminiService,   // 注入 AI 服務
            IHubContext<ChatHub> hubContext) // 注入 SignalR Hub 上下文
        {
            _context = context; // 將注入的實例存入私有欄位
            _geminiService = geminiService; // 將注入的實例存入私有欄位
            _hubContext = hubContext; // 將注入的實例存入私有欄位
        }

        // ==========================================
        // 1. [API] 取得對話列表
        // ==========================================
        // [HttpGet]: 定義為 HTTP GET 請求
        // "User/{userId}": 路由樣板，userId 為網址參數
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetSessions(int userId) // userId: 從網址取得的使用者 ID
        {
            // 使用 EF Core 查詢 ChatSession 資料表
            var sessions = await _context.ChatSession
                .Where(s => s.UserId == userId) // 過濾條件：只抓取該使用者的對話
                .OrderByDescending(s => s.UpdatedAt) // 排序：依照更新時間由新到舊 (最近聊的在上面)
                .Select(s => new // 投影 (Projection)：只選取前端需要的欄位，節省流量
                {
                    s.ChatSessionId, // 對話 ID
                    s.Title,         // 對話標題
                    // 格式化時間：如果有值則轉為字串，否則回傳空字串
                    UpdatedAt = s.UpdatedAt.HasValue ? s.UpdatedAt.Value.ToString("yyyy/MM/dd HH:mm") : ""
                })
                .ToListAsync(); // 非同步執行查詢並轉為 List

            return Ok(sessions); // 回傳 HTTP 200 OK 與 JSON 資料
        }

        // ==========================================
        // 2. [API] 建立新對話 (按 "+" 按鈕時)
        // ==========================================
        // [HttpPost]: 定義為 HTTP POST 請求，用於建立資源
        [HttpPost("Create")]
        public async Task<IActionResult> CreatChatSession([FromBody] ChatSessionDto request) // request: 從 Body 接收的 JSON 資料
        {
            // 建立一個新的 ChatSession 實體物件
            var session = new ChatSession
            {
                UserId = request.UserId, // 設定擁有者 ID (來自前端傳入)
                Title = "新對話",        // 設定預設標題，之後會由 AI 自動更新
                CreatedAt = DateTime.Now, // 設定建立時間為當下
                UpdatedAt = DateTime.Now  // 設定更新時間為當下
            };

            // 將新實體加入 EF Core 的追蹤清單 (尚未寫入資料庫)
            _context.ChatSession.Add(session);

            // 執行 SaveChangesAsync，將 SQL INSERT 指令發送到資料庫
            await _context.SaveChangesAsync();

            // 回傳 HTTP 200 OK，並包含新生成的 ID 與標題
            return Ok(new { sessionId = session.ChatSessionId, title = session.Title });
        }

        // ==========================================
        // 3. [API] ★ AI 自動產生標題 (進階功能)
        // ==========================================
        // [HttpPost]: 修改資料狀態使用 POST
        // "AutoTitle/{sessionId}": 路由包含對話 ID
        [HttpPost("AutoTitle/{sessionId}")]
        public async Task<IActionResult> AutoGenerateTitle(int sessionId) // sessionId: 目標對話 ID
        {
            // 1. 先從資料庫找出這個對話 Session
            var session = await _context.ChatSession.FindAsync(sessionId);

            // 如果找不到 (null)，回傳 404 Not Found
            if (session == null) return NotFound("找不到對話");

            // 2. 抓取該對話的第一則 User 訊息，用來判斷話題內容
            var firstMessage = await _context.ChatMessage
                .Where(m => m.SessionId == sessionId && m.Role == "user") // 條件：同個 Session 且角色是 User
                .OrderBy(m => m.CreatedAt) // 依照時間排序 (最舊的在前面)
                .FirstOrDefaultAsync();    // 取出第一筆，若無則回傳 null

            // 如果還沒有訊息，無法進行摘要，回傳 400 Bad Request
            if (firstMessage == null) return BadRequest("還沒有訊息，無法命名");

            try
            {
                // 3. 組合 Prompt (提示詞)，告訴 AI 要做什麼
                // interpolated string ($"...") 將使用者的訊息內容嵌入提示詞中
                string prompt = $"請根據以下使用者的發言，幫這個聊天室取一個「非常簡短」的標題(10個字以內)，不要有引號或額外文字：\n\n{firstMessage.Body}";

                // 呼叫 GeminiService 發送請求給 Google API，等待回傳結果
                string newTitle = await _geminiService.GetGeminiReplyAsync(prompt);

                // 4. 清理 AI 回傳的雜訊 (AI 有時候會自作聰明加上引號)
                newTitle = newTitle.Replace("\"", "") // 去除雙引號
                                   .Replace("「", "") // 去除左引號
                                   .Replace("」", "") // 去除右引號
                                   .Trim();           // 去除前後空白

                // 防呆機制：如果 AI 失控回傳太長，強制截斷為 50 字
                if (newTitle.Length > 50) newTitle = newTitle.Substring(0, 50);

                // 5. 更新資料庫中的 Session 實體
                session.Title = newTitle; // 更新標題欄位
                session.UpdatedAt = DateTime.Now; // 更新時間，讓這個對話在列表中浮到最上面

                // 儲存變更到資料庫
                await _context.SaveChangesAsync();

                // 6. (備註) 這裡可以透過 SignalR 通知前端更新標題，目前先註解掉
                // await _hubContext.Clients.User(session.UserId.ToString()).SendAsync("SessionTitleUpdated", sessionId, newTitle);

                // 回傳成功結果與新標題
                return Ok(new { sessionId, newTitle });
            }
            catch (Exception ex)
            {
                // 錯誤處理：若 AI 服務掛掉或網路錯誤，回傳 500
                return StatusCode(500, $"AI 命名失敗: {ex.Message}");
            }
        }



        // ==========================================
        // 4. [API] 刪除對話
        // ==========================================
        // [HttpDelete]: 定義為 HTTP DELETE 請求
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteSession(int id) // id: 要刪除的 SessionId
        {
            // 尋找目標 Session
            var session = await _context.ChatSession.FindAsync(id);
            // 若不存在則回傳 404
            if (session == null) return NotFound("找不到該對話");

            // 1. 刪除關聯的訊息 (Cascade Delete 手動處理)
            // 先找出所有屬於此 Session 的訊息
            var messages = _context.ChatMessage.Where(m => m.SessionId == id);
            // 從 Context 中移除這些訊息
            _context.ChatMessage.RemoveRange(messages);

            // 2. 刪除對話本身
            _context.ChatSession.Remove(session);

            // 一次執行所有刪除操作
            await _context.SaveChangesAsync();

            // 回傳成功訊息
            return Ok(new { success = true, message = "刪除成功" });
        }
    }
}