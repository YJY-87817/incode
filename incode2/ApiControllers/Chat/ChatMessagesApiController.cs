using incode2.Models;
using incode2.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using incode2.Dtos.Chat;
using incode2.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace incode2.ApiControllers.Chat
{
    [RequireHttps]
    [Authorize]
    public class ChatMessagesApiController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly GeminiService _geminiService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ChatMessagesApiController(
            incodedatabaseContext context,
            GeminiService geminiService,
            IHubContext<ChatHub> hubContext,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _geminiService = geminiService;
            _hubContext = hubContext;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // ★ 私有 Helper 方法 (解決重複代碼問題)
        // ==========================================

        private int GetCurrentUserId()   //定義取得使用者方法，回傳值為整數
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier); // 宣告變數 userIdStr，並從當前使用者的「身分證 (User Claims)」中，尋找標記為 "NameIdentifier" (通常是 ID) 的欄位值。

            if (int.TryParse(userIdStr, out int userId)) return userId; // 嘗試將剛剛抓到的字串(userIdStr)轉換成整數，成功則將結果放入 out userId，回傳轉換後的整數userId

            return 0; // 如果上述轉換失敗，回傳 0
        }

        private async Task<string> GetSystemPromptAsync()   //取得系統Prompt的非同步方法
        {
            try
            {
                string promptPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Prompts", "Prompts.md"); //嘗試宣告一個文字類型的變數 promptPath，把後面計算出來的路徑文字存進去
                if (System.IO.File.Exists(promptPath))  // 如果系統裡有這個檔案路徑
                {
                    return await System.IO.File.ReadAllTextAsync(promptPath); //就回傳並等待讀取非同步文字的結果
                }
            }
            catch { /* log error */ } //如果上面發生異常，這裡會紀錄發生啥錯誤
            return "你是一個專業的電影評論家與推薦系統。如果使用者的問題與電影、影集、演員無關，請禮貌地拒絕回答。"; //如果檔案讀取過程發生錯誤，就回傳這個預設值
        }

        private async Task EnsureGroupAsync(string connectionId, string sessionIdStr)  // 確保連線ID加入群組
        {
            if (!string.IsNullOrEmpty(connectionId))  //如果文字類型的變數不是空的(connectionId)  //!string.IsNullOrEmpty不是空的嗎？（代表裡面有東西）
            {
                await _hubContext.Groups.AddToGroupAsync(connectionId, sessionIdStr); // 就等待將這個「連線(Connection)」加入到指定的「群組(Session)」中
            }
        }

        // ★ [使用者專用] 當前端給我 [使用者ID] 和 [房間ID] 時
        // 我會去處理(找舊的或開新的)，最後打包回傳兩樣結果 (聊天室物件, 是否為新建)
        private async Task<(ChatSession Session, bool IsNew)> GetOrInitSessionAsync(int userId, int requestedSessionId, string initialTitle = "我的對話")
        {
            ChatSession session = null; // 宣告一個變數 session 用來存放等一下抓到的聊天室資料，預設為空

            // 1. 如果前端有傳 SessionId，先試著找找看
            if (requestedSessionId > 0)  // 檢查使用者是否指定大於0的聊天室ID
            {
                session = await _context.ChatSession.FindAsync(requestedSessionId); //去資料庫根據 requestedSessionId 尋找對應的聊天室物件session (等待非同步查詢結果)

                // 安全檢查：如果有找到聊天室，但這個聊天室的主人不是當前使用者，就視為沒找到 (回傳 null 讓下面邏輯去開新的)
                if (session != null && session.UserId != userId) return (null, false);
            }

            // 2. 如果沒傳 ID (0) 或者上面沒找到，就自動找該用戶「最新」的 Session (接續上次)
            if (session == null)
            {
                session = await _context.ChatSession
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            // 3. 真的沒有 (第一次使用 / 資料庫空的) -> 建立新的
            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = userId,
                    Title = initialTitle,
                    Ishumanmode = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.ChatSession.Add(session);
                await _context.SaveChangesAsync();
                return (session, true); // 回傳：是新的 (IsNew = true)
            }

            session.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(); // 儲存時間與模式重設
            return (session, false);
        }

        // ==========================================
        // API Actions
        // ==========================================

        // 1. [使用者 API] 取得或建立唯一對話
        [HttpPost]
        [Route("api/ChatMessages/GetOrCreateSession")]
        public async Task<IActionResult> GetOrCreateSession()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("請先登入");

            try
            {
                // ★ 改用 Helper，一行搞定：自動找舊的，沒有就開新的
                var result = await GetOrInitSessionAsync(userId, 0);

                return Ok(new { sessionId = result.Session.ChatSessionId, isNew = result.IsNew });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"建立對話失敗: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("api/ChatMessages/SendMessage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                // 1. 取得 Session：自動接續舊房或開新房
                var (currentSession, _) = await GetOrInitSessionAsync(userId, request.SessionId,
                    request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent);

                if (currentSession == null) return Unauthorized("您無權存取此對話");

                string sessionIdStr = currentSession.ChatSessionId.ToString();
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);

                // 2. 處理 User 訊息：生成向量並存檔
                float[] userVector = await _geminiService.GetEmbeddingAsync(request.UserContent);
                var userMsg = new ChatMessage
                {
                    SessionId = currentSession.ChatSessionId,
                    Role = "user",
                    Body = request.UserContent,
                    Embedding = JsonSerializer.Serialize(userVector),
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(userMsg);
                await _context.SaveChangesAsync();

                // 3. 廣播 User 訊息
                var sessionGroup = _hubContext.Clients.Group(sessionIdStr);
                await sessionGroup.SendAsync("ReceiveMessage", "User", request.UserContent);

                // ★ 關鍵：如果是真人客服模式，AI 直接結束，不介入
                if (currentSession.Ishumanmode == true)
                {
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("ReceiveSupportRequest", currentSession.ChatSessionId, request.UserContent);
                    return Ok(new { success = true, mode = "live", sessionId = currentSession.ChatSessionId });
                }

                // 4. AI 模式處理
                string aiResponseText = "";
                string messageType = "AI";

                if (request.UserContent.Contains("生圖") || request.UserContent.Contains("產生圖片"))
                {
                    string imageDescription = request.UserContent.Replace("生圖", "").Replace("產生圖片", "").Trim();
                    aiResponseText = await _geminiService.GenerateImageAsync(imageDescription);
                    messageType = "AI_Image";
                }
                else
                {
                    // 向量檢索 (RAG)
                    string vectorContext = await GetVectorContextAsync(request.UserContent);
                    string basePrompt = await GetSystemPromptAsync();

                    string finalPrompt = $@"{basePrompt}
                    ---
                    【嚴格規則】:
                    1. 你「只能」根據下方提供的【資料庫檢索內容】回答。
                    2. 若內容中找不到答案，請回答：「抱歉，根據資料庫目前的紀錄，我無法回答這個問題。」
                    3. 嚴禁使用你的外部訓練知識庫。
                    
                    【資料庫檢索內容】:
                    {vectorContext}
                    
                    使用者問題: {request.UserContent}";

                    aiResponseText = await _geminiService.GetGeminiReplyAsync(finalPrompt);
                }

                // 5. 存 AI 訊息與向量
                float[] aiVector = await _geminiService.GetEmbeddingAsync(aiResponseText);
                var aiMsg = new ChatMessage
                {
                    SessionId = currentSession.ChatSessionId,
                    Role = "assistant",
                    Body = aiResponseText,
                    Embedding = JsonSerializer.Serialize(aiVector),
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(aiMsg);
                await _context.SaveChangesAsync();

                // 6. 廣播 AI 回應
                await sessionGroup.SendAsync("ReceiveMessage", messageType, aiResponseText);
                return Ok(new { success = true, sessionId = currentSession.ChatSessionId, mode = "ai" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = $"伺服器錯誤: {ex.Message}" }); }
        }

        // ==========================================================
        // ★ [客服專用 API] 取得所有等待中的對話列表 (新功能)
        // ==========================================================
        [HttpGet]
        [Route("api/ChatMessages/GetActiveSupportSessions")]
        public async Task<IActionResult> GetActiveSupportSessions()
        {
            // 這裡建議加上角色權限驗證，例如 if (!User.IsInRole("Admin")) return Forbid();
            if (GetCurrentUserId() == 0) return Unauthorized();

            try
            {
                // 客服不需要自動建立 Session，而是撈取已經存在的、且 Ishumanmode 為 true 的 Session
                var sessions = await _context.ChatSession
                    .Where(s => s.Ishumanmode == true) // ★ 只撈取正在請求真人支援的
                    .OrderByDescending(s => s.UpdatedAt) // 最新的在上面
                    .Select(s => new
                    {
                        s.ChatSessionId,
                        s.UserId,
                        s.Title,
                        LastUpdate = s.UpdatedAt.HasValue ? s.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm") : "",
                        // 順便撈最後一則訊息當預覽，讓客服知道客戶在問什麼
                        LastMessage = _context.ChatMessage
                                        .Where(m => m.SessionId == s.ChatSessionId)
                                        .OrderByDescending(m => m.CreatedAt)
                                        .Select(m => m.Body)
                                        .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // 客服回覆
        [HttpPost]
        [Route("api/ChatMessages/AdminReply")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AdminReply([FromBody] MessageRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (GetCurrentUserId() == 0) return Unauthorized();
            if (request.SessionId <= 0) return BadRequest("無效的 Session ID");

            try
            {
                string sessionIdStr = request.SessionId.ToString();
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);

                // ★ 新增：將客服的回覆也轉成向量，讓 AI 以後能學習客服的回答
                float[] adminVector = await _geminiService.GetEmbeddingAsync(request.UserContent);

                var adminMsg = new ChatMessage
                {
                    SessionId = request.SessionId,
                    Role = "assistant", // 客服與 AI 統一使用 assistant 角色
                    Body = request.UserContent,
                    Embedding = JsonSerializer.Serialize(adminVector), // 存入向量
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(adminMsg);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group(sessionIdStr)
                    .SendAsync("ReceiveMessage", "Live", request.UserContent);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"客服回覆失敗: {ex.Message}" });
            }
        }

        // 切換模式 (呼叫客服)
        [HttpPost]
        [Route("api/ChatMessages/SwitchMode")]
        public async Task<IActionResult> SwitchMode([FromBody] SwitchModeDto request)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("請先登入");

            try
            {
                var session = await _context.ChatSession.FindAsync(request.SessionId);

                // 1. Session 處理與權限檢查
                if (session == null)
                {
                    var result = await GetOrInitSessionAsync(userId, request.SessionId);
                    session = result.Session;
                }
                else if (session.UserId != userId)
                {
                    return Unauthorized();
                }

                // 2. 更新模式與時間
                session.Ishumanmode = request.Ishumanmode;
                session.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                string modeName = request.Ishumanmode ? "客服人員" : "AI";
                string sessionIdStr = session.ChatSessionId.ToString();

                // 3. SignalR 通知
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);
                var sessionGroup = _hubContext.Clients.Group(sessionIdStr);

                await sessionGroup.SendAsync("ReceiveMessage", "System", $"已切換至 {modeName} 模式");
                await sessionGroup.SendAsync("UpdateModeState", session.ChatSessionId, request.Ishumanmode);

                // 4. 若切換為真人，處理向量存檔與通知
                if (request.Ishumanmode)
                {
                    string autoReplyText = "收到您的請求，請稍等 1-2 分鐘，客服人員將為您服務。";
                    float[] autoVector = await _geminiService.GetEmbeddingAsync(autoReplyText);

                    _context.ChatMessage.Add(new ChatMessage
                    {
                        SessionId = session.ChatSessionId,
                        Role = "assistant",
                        Body = autoReplyText,
                        Embedding = JsonSerializer.Serialize(autoVector),
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    await sessionGroup.SendAsync("ReceiveMessage", "Live", autoReplyText);
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("RefreshSessions");
                }

                // ★ 你指定的唯一回傳行
                return Ok(new { success = true, mode = modeName, sessionId = session.ChatSessionId });
            }
            catch (Exception ex)
            {
                // 確保 catch 也有回傳，避免編譯錯誤
                return StatusCode(500, new { error = ex.Message });
            }
        }



        // 上傳圖片
        [HttpPost]
        [Route("api/ChatMessages/UploadImage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();
            if (request.Image == null || request.Image.Length == 0) return BadRequest("未上傳圖片");

            var (currentSession, isNew) = await GetOrInitSessionAsync(userId, request.SessionId, "圖片對話");
            int validSessionId = currentSession.ChatSessionId;
            string validSessionIdStr = validSessionId.ToString();

            try
            {
                // 1. 圖片存檔
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "chat");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.Image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await request.Image.CopyToAsync(fileStream); }
                string dbRelativeUrl = $"/img/chat/{uniqueFileName}";

                // 呼叫imgapi 
                string systemPrompt = await GetSystemPromptAsync();
                string userMessage = string.IsNullOrWhiteSpace(request.UserContent) ? "請分析這張圖" : request.UserContent;
                string fullPrompt = $"{systemPrompt}\n\n[圖片分析請求]\n使用者說明：{userMessage}";

                // ★ 呼叫 Gemini 2.5 Pro
                string aiReplyText = await _geminiService.AnalyzeImageAsync(request.Image, fullPrompt);

                // 3. 資料庫存檔
                await EnsureGroupAsync(request.ConnectionId, validSessionIdStr);
                _context.ChatMessage.Add(new ChatMessage { SessionId = validSessionId, Role = "user", Body = $"<img src='{dbRelativeUrl}' /> {request.UserContent}", CreatedAt = DateTime.Now });
                _context.ChatMessage.Add(new ChatMessage { SessionId = validSessionId, Role = "assistant", Body = aiReplyText, CreatedAt = DateTime.Now });
                await _context.SaveChangesAsync();

                // 4. SignalR 推播
                await _hubContext.Clients.Group(validSessionIdStr).SendAsync("ReceiveMessage", "User_Image", dbRelativeUrl);
                await _hubContext.Clients.Group(validSessionIdStr).SendAsync("ReceiveMessage", "AI", aiReplyText);

                if (currentSession.Ishumanmode == true)
                {
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("ReceiveSupportRequest", validSessionId, "(傳送了圖片)");
                }

                return Ok(new { success = true, sessionId = validSessionId, isNew = isNew });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // 取得歷史訊息
        [HttpGet]
        [Route("api/ChatMessages/GetHistory/{sessionId}")]
        public async Task<IActionResult> GetHistory(int sessionId)
        {
            if (GetCurrentUserId() == 0) return Unauthorized();


            try
            {
                var history = await _context.ChatMessage
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
                return Ok(history);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // 在 ChatMessagesApiController 中新增/修改方法

        private async Task<string> GetVectorContextAsync(string userContent)
        {
            // 1. 呼叫 GeminiService 取得問題的 Embedding (數字陣列)
            float[] queryVector = await _geminiService.GetEmbeddingAsync(userContent);

            // 2. 從資料庫撈出有向量資料的訊息 (排除系統訊息)
            var messagesWithVector = await _context.ChatMessage
                .AsNoTracking()
                .Where(m => m.Embedding != null && m.Role != "system")
                .ToListAsync();

            // 3. 在記憶體中計算餘弦相似度 (Cosine Similarity)
            var topMatches = messagesWithVector
                .Select(m => new {
                    m.Body,
                    m.Role,
                    Similarity = CalculateCosineSimilarity(queryVector, JsonSerializer.Deserialize<float[]>(m.Embedding))
                })
                .Where(x => x.Similarity > 0.7) // 相似度門檻，可依需求調整
                .OrderByDescending(x => x.Similarity)
                .Take(5) // 只取最相關的前 5 筆
                .ToList();

            if (!topMatches.Any()) return "資料庫中無直接相關參考內容。";

            return string.Join("\n", topMatches.Select(x => $"[{x.Role}]: {x.Body}"));
        }

        // 餘弦相似度計算公式
        private float CalculateCosineSimilarity(float[] vecA, float[] vecB)
        {
            if (vecA.Length != vecB.Length) return 0;
            float dotProduct = 0, magA = 0, magB = 0;
            for (int i = 0; i < vecA.Length; i++)
            {
                dotProduct += vecA[i] * vecB[i];
                magA += vecA[i] * vecA[i];
                magB += vecB[i] * vecB[i];
            }
            return dotProduct / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
        }

        // 檢查登入狀態
        [HttpGet]
        [Route("api/ChatMessages/CheckLogin")]
        [AllowAnonymous]
        public IActionResult CheckLogin()
        {
            return Ok(new { isLoggedIn = User.Identity.IsAuthenticated });
        }
    }
}