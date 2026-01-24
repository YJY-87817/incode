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
            // 讀取 Prompts 資料夾下的 MovieBot.md
            string promptPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Prompts", "MovieBot.md");

            if (System.IO.File.Exists(promptPath))
            {
                return await System.IO.File.ReadAllTextAsync(promptPath);
            }
            // 預設 Prompt，如果檔案讀取失敗
            return "你是一個專業的電影評論家與推薦系統。如果使用者的問題與電影、影集、演員無關，請禮貌地拒絕回答。";
        }

        // ==========================================
        // 1. [API] 切換模式 (真人 / AI)
        // ==========================================
        [HttpPost]
        [Route("api/ChatMessages/SwitchMode")]
        public async Task<IActionResult> SwitchMode([FromBody] SwitchModeDto request)
        {
            // 確保 ChatSession Model 有 Ishumanmode 欄位
            var session = await _context.ChatSession.FindAsync(request.SessionId);
            if (session == null) return NotFound("找不到對話紀錄");

            // true = 真人, false = AI
            session.Ishumanmode = request.Ishumanmode;

            await _context.SaveChangesAsync();

            string modeName = request.Ishumanmode ? "真人客服" : "AI 助理";

            // 通知前端模式已變更
            if (!string.IsNullOrEmpty(request.ConnectionId))
            {
                await _hubContext.Clients.Client(request.ConnectionId)
                    .SendAsync("ReceiveMessage", "System", $"已切換至 {modeName} 模式");
            }

            return Ok(new { success = true, mode = modeName });
        }

        // ==========================================
        // 2. [API] 文字對話 (整合電影詢問 & 生圖)
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
                long currentSessionId = request.SessionId;
                ChatSession currentSession = null;

                if (currentSessionId <= 0)
                {
                    currentSession = new ChatSession
                    {
                        Title = request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent,
                        CreatedAt = DateTime.Now,
                        UserId = 1, // TODO: 替換為真實 UserID
                        Ishumanmode = false // 預設為 AI 模式 (false)
                    };
                    _context.ChatSession.Add(currentSession);
                    await _context.SaveChangesAsync();
                    currentSessionId = currentSession.ChatSessionId;
                }
                else
                {
                    currentSession = await _context.ChatSession.FindAsync(currentSessionId);
                }

                // B. 存檔 - 使用者訊息
                var userMsg = new ChatMessage
                {
                    SessionId = (int)currentSessionId,
                    Role = "user",
                    Body = request.UserContent,
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(userMsg);
                await _context.SaveChangesAsync();

                // C. 判斷模式：真人 (Ishumanmode = true) vs AI (Ishumanmode = false)
                if (currentSession.Ishumanmode == true)
                {
                    // === 真人模式 ===
                    // 1. 不呼叫 AI
                    // 2. 透過 SignalR 通知客服群組 (假設有一個 Group 叫 "SupportAgents")
                    await _hubContext.Clients.Group("SupportAgents")
                        .SendAsync("ReceiveSupportRequest", currentSessionId, request.UserContent);

                    // 3. 回傳成功，告知前端等待客服
                    return Ok(new { success = true, sessionId = currentSessionId, mode = "live", message = "已傳送給客服人員" });
                }
                else
                {
                    // === AI 模式 ===
                    string aiResponseText = "";
                    string messageType = "AI"; // SignalR 類型：文字(AI) 或 圖片(AI_Image)

                    // 讀取 Prompt 設定檔
                    string systemPrompt = await GetSystemPromptAsync();

                    // D. 判斷是否為「生圖」指令
                    if (request.UserContent.StartsWith("生圖") || request.UserContent.Contains("產生圖片"))
                    {
                        // 呼叫 Service 的生圖方法 (需在 GeminiService 實作)
                        string imageDescription = request.UserContent.Replace("生圖", "").Trim();
                        // 假設回傳圖片 URL
                        string imageUrl = await _geminiService.GenerateImageAsync(imageDescription);

                        aiResponseText = $"<img src='{imageUrl}' alt='Generated Image' />";
                        messageType = "AI_Image";
                    }
                    else
                    {
                        // E. 呼叫 AI 文字服務 (電影問答)
                        // 組合 Prompt
                        string fullPrompt = $"{systemPrompt}\n\n使用者問題：{request.UserContent}";
                        aiResponseText = await _geminiService.GetGeminiReplyAsync(fullPrompt);
                    }

                    // F. 存檔 - AI 回覆
                    var aiMsg = new ChatMessage
                    {
                        SessionId = (int)currentSessionId,
                        Role = "assistant",
                        Body = aiResponseText,
                        CreatedAt = DateTime.Now
                    };
                    _context.ChatMessage.Add(aiMsg);
                    await _context.SaveChangesAsync();

                    // G. SignalR 推播
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
        // 3. [API] 以圖搜圖 (電影辨識) - 保持不變
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
                // A. 通知前端
                if (!string.IsNullOrEmpty(request.ConnectionId))
                {
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "System", "正在分析圖片中的電影資訊...");
                }

                // B. 檔案處理
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

                    // C. 呼叫 Gemini Vision API (搜圖)
                    string prompt = "這是一張使用者上傳的圖片。請辨識這是否為電影海報或劇照？如果是，請告訴我電影名稱、年份以及簡短劇情介紹。如果不是電影相關圖片，請描述圖片內容並告知使用者你只能回答電影相關問題。";
                    string aiReply = await _geminiService.GetGeminiImageReplyAsync(base64Image, imageFile.ContentType, prompt);

                    // D. 寫入 UserImage
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

                    // E. 寫入 ChatMessage
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

                    // F. SignalR 推播
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
    }

    // 這裡的 DTO 也順便更新名稱
    public class SwitchModeDto
    {
        public int SessionId { get; set; }
        public bool Ishumanmode { get; set; } // 修改這裡
        public string ConnectionId { get; set; }
    }
}