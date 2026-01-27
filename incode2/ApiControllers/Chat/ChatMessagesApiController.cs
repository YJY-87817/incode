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

namespace incode2.ApiControllers.Chat
{
    [RequireHttps]
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
        // 0. [Helper] 讀取 AI 角色設定 (Prompt)
        // ==========================================
        private async Task<string> GetSystemPromptAsync()
        {
            try
            {
                // 讀取 Prompts 資料夾下的 Prompts.md
                string promptPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Prompts", "Prompts.md");

                if (System.IO.File.Exists(promptPath))
                {
                    return await System.IO.File.ReadAllTextAsync(promptPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"讀取 Prompt 失敗: {ex.Message}");
            }
            // 預設 Prompt
            return "你是一個專業的電影評論家與推薦系統。如果使用者的問題與電影、影集、演員無關，請禮貌地拒絕回答。";
        }

        // ==========================================
        // 1. [API] 切換模式 (真人 / AI)
        // ==========================================
        [HttpPost]
        [Route("api/ChatMessages/SwitchMode")]
        public async Task<IActionResult> SwitchMode([FromBody] SwitchModeDto request)
        {
            // 嘗試尋找 Session
            var session = await _context.ChatSession.FindAsync(request.SessionId);

            // 如果找不到 (session == null)，直接建立一個新的！
            if (session == null)
            {
                session = new ChatSession
                {
                    // 如果是用戶第一次來，可能沒有 UserID，預設給 1
                    UserId = 1,
                    Title = "新對話 " + DateTime.Now.ToString("MM/dd HH:mm"),
                    Ishumanmode = request.Ishumanmode, // 直接設定成請求的模式
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ChatSession.Add(session);
                await _context.SaveChangesAsync();

                // 更新 request 的 SessionId，因為等一下 SignalR 可能會用到
                request.SessionId = session.ChatSessionId;
            }
            else
            {
                // 如果找到了，就照常更新
                session.Ishumanmode = request.Ishumanmode;
                await _context.SaveChangesAsync();
            }

            string modeName = request.Ishumanmode ? "真人客服" : "AI 助理";

            // 通知前端 (SignalR)
            if (!string.IsNullOrEmpty(request.ConnectionId))
            {
                // 先把 Connection 加入這個新 Session 的群組 (防呆)
                await _hubContext.Groups.AddToGroupAsync(request.ConnectionId, session.ChatSessionId.ToString());

                await _hubContext.Clients.Client(request.ConnectionId)
                    .SendAsync("ReceiveMessage", "System", $"已切換至 {modeName} 模式");
            }

            // ★ 回傳新的 sessionId 給前端，讓前端更新 LocalStorage
            return Ok(new { success = true, mode = modeName, sessionId = session.ChatSessionId });
        }

        // ==========================================
        // 2. [API] 文字對話 (使用者發送 -> 觸發 AI 或 真人通知)
        // ==========================================
        [HttpPost]
        [Route("api/ChatMessages/SendMessage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // A. 處理 Session
                int currentSessionId = request.SessionId;
                ChatSession currentSession = null;

                if (currentSessionId > 0)
                {
                    currentSession = await _context.ChatSession.FindAsync(currentSessionId);
                }

                // 如果 ID 是 0，或者「有 ID 但資料庫找不到 (null)」，就建立新的
                if (currentSession == null)
                {
                    currentSession = new ChatSession
                    {
                        // 取訊息前 20 字當標題
                        Title = request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        UserId = 1, // TODO: 之後改成真實 UserID
                        Ishumanmode = false // 預設 AI 模式
                    };
                    _context.ChatSession.Add(currentSession);
                    await _context.SaveChangesAsync();

                    // 更新目前的 ID，讓後續程式碼使用這個新的
                    currentSessionId = currentSession.ChatSessionId;
                }
                // B. 存檔 - 使用者訊息 (Role = user)
                var userMsg = new ChatMessage
                {
                    SessionId = currentSessionId,
                    Role = "user",
                    Body = request.UserContent,
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(userMsg);
                await _context.SaveChangesAsync();

                // C. 判斷模式
                if (currentSession.Ishumanmode == true)
                {
                    // === 真人模式 ===
                    // 通知客服群組有新訊息
                    await _hubContext.Clients.Group("SupportAgents")
                        .SendAsync("ReceiveSupportRequest", currentSessionId, request.UserContent);

                    return Ok(new { success = true, sessionId = currentSessionId, mode = "live", message = "已傳送給客服人員" });
                }
                else
                {
                    // === AI 模式 ===
                    string aiResponseText = "";
                    string messageType = "AI";

                    string systemPrompt = await GetSystemPromptAsync();

                    // D. 判斷生圖
                    if (request.UserContent.StartsWith("生圖") || request.UserContent.Contains("產生圖片"))
                    {
                        string imageDescription = request.UserContent.Replace("生圖", "").Trim();
                        string imageUrl = await _geminiService.GenerateImageAsync(imageDescription);

                        aiResponseText = imageUrl;
                        if (!aiResponseText.StartsWith("<"))
                        {
                            aiResponseText = $"<img src='{imageUrl}' alt='Generated Image' />";
                        }

                        messageType = "AI_Image";
                    }
                    else
                    {
                        // E. 文字問答
                        string fullPrompt = $"{systemPrompt}\n\n使用者問題：{request.UserContent}";
                        aiResponseText = await _geminiService.GetGeminiReplyAsync(fullPrompt);
                    }

                    // F. 存檔 - AI 回覆
                    var aiMsg = new ChatMessage
                    {
                        SessionId = currentSessionId,
                        Role = "assistant",
                        Body = aiResponseText,
                        CreatedAt = DateTime.Now
                    };
                    _context.ChatMessage.Add(aiMsg);
                    await _context.SaveChangesAsync();

                    // G. SignalR 推播給使用者
                    if (!string.IsNullOrEmpty(request.ConnectionId))
                    {
                        await _hubContext.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveMessage", messageType, aiResponseText);
                    }

                    // H. 回傳 DTO
                    var responseDto = new ChatMessageDto
                    {
                        ChatMessageId = aiMsg.ChatMessageId,
                        SessionId = aiMsg.SessionId,
                        Role = aiMsg.Role,
                        Body = aiResponseText,
                        CreatedAt = aiMsg.CreatedAt.HasValue ? aiMsg.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    return Ok(new { success = true, sessionId = currentSessionId, mode = "ai", data = responseDto });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"伺服器錯誤: {ex.Message}" });
            }
        }

        // ==========================================
        // 3. [API] 以圖搜圖 (電影辨識)
        // ==========================================
        [HttpPost]
        [Route("api/ChatMessages/UploadImage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            var imageFile = request.Image;
            if (imageFile == null || imageFile.Length == 0) return BadRequest("未上傳圖片");

            try
            {
                if (!string.IsNullOrEmpty(request.ConnectionId))
                {
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "System", "正在分析圖片中的電影資訊...");
                }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/chat");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string extension = Path.GetExtension(imageFile.FileName);
                string shortFileName = $"{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";
                string fullFilePath = Path.Combine(uploadsFolder, shortFileName);
                string dbRelativeUrl = $"/img/chat/{shortFileName}";

                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
                    {
                        memoryStream.Position = 0;
                        await memoryStream.CopyToAsync(fileStream);
                    }

                    memoryStream.Position = 0;
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64Image = Convert.ToBase64String(imageBytes);

                    string prompt = "這是一張使用者上傳的圖片。請辨識這是否為電影海報或劇照？如果是，請告訴我電影名稱、年份以及簡短劇情介紹。如果不是電影相關圖片，請描述圖片內容並告知使用者你只能回答電影相關問題。";
                    string aiReply = await _geminiService.GetGeminiImageReplyAsync(base64Image, imageFile.ContentType, prompt);

                    var newImage = new UserImage
                    {
                        UserId = 1,
                        ImageUrl = dbRelativeUrl,
                        Caption = aiReply.Length > 200 ? aiReply.Substring(0, 197) + "..." : aiReply,
                        DisplayOrder = 0,
                        CreatedAt = DateTime.Now
                    };
                    _context.UserImage.Add(newImage);
                    await _context.SaveChangesAsync();

                    if (request.SessionId > 0)
                    {
                        var userMsg = new ChatMessage
                        {
                            SessionId = request.SessionId,
                            Role = "user",
                            Body = $"<img src='{dbRelativeUrl}' alt='uploaded image' style='max-width: 200px; border-radius: 8px;' />",
                            CreatedAt = DateTime.Now
                        };
                        _context.ChatMessage.Add(userMsg);

                        var aiMsg = new ChatMessage
                        {
                            SessionId = request.SessionId,
                            Role = "assistant",
                            Body = aiReply,
                            CreatedAt = DateTime.Now
                        };
                        _context.ChatMessage.Add(aiMsg);
                        await _context.SaveChangesAsync();
                    }

                    if (!string.IsNullOrEmpty(request.ConnectionId))
                    {
                        await _hubContext.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveMessage", "User_Image", dbRelativeUrl);
                        await _hubContext.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveMessage", "AI", aiReply);
                    }

                    return Ok(new { success = true, url = dbRelativeUrl, reply = aiReply });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // 4. [API] 取得歷史訊息
        // ==========================================
        [HttpGet]
        [Route("api/ChatMessages/GetHistory/{sessionId}")]
        public async Task<IActionResult> GetHistory(int sessionId)
        {
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
                        CreatedAt = m.CreatedAt.HasValue
                                    ? m.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                    : ""
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // 5. [API] ★ 新增: 客服回覆專用 (AdminReply)
        // ==========================================
        [HttpPost]
        [Route("api/ChatMessages/AdminReply")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AdminReply([FromBody] MessageRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // 1. 寫入資料庫：身分標記為 assistant (代表客服)
                var adminMsg = new ChatMessage
                {
                    SessionId = request.SessionId,
                    Role = "assistant", // ★ 關鍵：設為 assistant，前端顯示在左邊(或對應位置)，且不會觸發 AI
                    Body = request.UserContent, // 這裡的 UserContent 其實是管理員的回覆內容
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(adminMsg);
                await _context.SaveChangesAsync();

                // 2. 推播給前台使用者 (透過 SessionId 群組)
                // 確保前台 Vue 在連線時有呼叫 connection.invoke("JoinSessionGroup", sessionId)
                string groupName = request.SessionId.ToString();

                // SendAsync("ReceiveMessage", "Live", 內容) -> "Live" 讓前端知道這是真人
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("ReceiveMessage", "Live", request.UserContent);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"客服回覆失敗: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("api/ChatMessages/CreateSession")]
        public async Task<IActionResult> CreateSession([FromBody] ChatSessionDto request)
        {
            try
            {
                // 1. 建立新對話物件
                var newSession = new ChatSession
                {
                    // 若前端有傳 UserId 就用，沒有就預設 1
                    UserId = (request != null && request.UserId > 0) ? request.UserId : 1,
                    Title = "新對話 " + DateTime.Now.ToString("MM/dd HH:mm"),
                    Ishumanmode = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 2. 存入資料庫
                _context.ChatSession.Add(newSession);
                await _context.SaveChangesAsync();

                // 3. 回傳新產生的 ID 給前端
                return Ok(new { sessionId = newSession.ChatSessionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"建立對話失敗: {ex.Message}" });
            }
        }
    }
}