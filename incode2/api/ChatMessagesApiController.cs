using incode2.Data;
using incode2.Models;
using incode2.Services;
using incode2.DTO;
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

namespace incode2.Controllers
{
    // 強制使用 HTTPS，增加安全性
    [RequireHttps] // 屬性：要求該控制器所有請求必須使用 HTTPS 協議，提升傳輸安全性
    public class ChatMessagesApiController : Controller // 定義聊天室 API 控制器，繼承 ASP.NET Core 基礎 Controller 類別
    {
        // 宣告私有唯讀欄位，透過建構子注入依賴服務
        private readonly incodedatabaseContext _context; // 資料庫上下文：用於操作資料庫（EF Core）
        private readonly GeminiService _geminiService; // Gemini AI 服務：處理 AI 文字/圖片回覆邏輯
        private readonly IHubContext<ChatHub> _hubContext; // SignalR Hub 上下文：用於即時通訊推播訊息
        private readonly IWebHostEnvironment _webHostEnvironment; // Web 主機環境：用於獲取網站根目錄路徑

        // 建構子：依賴注入（DI）初始化各項服務
        public ChatMessagesApiController(
            incodedatabaseContext context, // 注入資料庫上下文實例
            GeminiService geminiService, // 注入 Gemini AI 服務實例
            IHubContext<ChatHub> hubContext, // 注入 SignalR Hub 上下文實例
            IWebHostEnvironment webHostEnvironment) // 注入 Web 主機環境實例
        {
            // 將注入的實例賦值給私有欄位，供控制器方法使用
            _context = context;
            _geminiService = geminiService;
            _hubContext = hubContext;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. [API] 文字對話 (已移除忘記密碼邏輯)
        // ==========================================
        [HttpPost] // HTTP 方法：指定該方法響應 POST 請求
        [Route("api/ChatMessages/SendMessage")] // 路由：定義 API 訪問路徑
        [IgnoreAntiforgeryToken] // 屬性：忽略防偽令牌驗證（適用於前後端分離場景）
        // 非同步方法：處理使用者發送文字訊息請求，參數透過 Request Body 接收 MessageRequestDto 物件
        public async Task<IActionResult> SendMessage([FromBody] MessageRequestDto request)
        {
            // 驗證 DTO (包含檢查 ConnectionId)
            if (!ModelState.IsValid) return BadRequest(ModelState); // 驗證請求參數合法性，不合法則返回 400 錯誤 + 驗證訊息

            try // 異常處理區塊：捕獲程式執行中的例外
            {
                // A. 處理 Session (若 ID <= 0 則新增對話 Session)
                long currentSessionId = request.SessionId; // 宣告變數：當前對話 Session ID，從請求 DTO 獲取
                if (currentSessionId <= 0) // 判斷：若 Session ID 無效（<=0），表示需要建立新的對話 Session
                {
                    var newSession = new ChatSession // 建立新的 ChatSession 實體（對話會話記錄）
                    {
                        // 標題取前 20 個字：將使用者輸入的內容截取前20字作為對話標題
                        Title = request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent,
                        CreatedAt = DateTime.Now, // 建立時間：當前伺服器時間
                        UserId = 1 // TODO: 請改為 User.Identity.GetUserId() 或相關邏輯（暫時寫死為1，需替換為真實使用者ID）
                    };
                    _context.ChatSession.Add(newSession); // 將新 Session 加入 EF Core 追蹤（準備插入資料庫）
                    await _context.SaveChangesAsync(); // 非同步保存變更：將新 Session 寫入資料庫
                    currentSessionId = newSession.ChatSessionId; // 更新當前 Session ID 為新建立的 ID
                }

                // B. 存檔 - 使用者訊息
                var userMsg = new ChatMessage // 建立使用者訊息實體
                {
                    SessionId = (int)currentSessionId, // 所屬對話 Session ID（轉換為 int 類型）
                    Role = "user", // 角色：標記為使用者發送的訊息
                    Body = request.UserContent, // 訊息內容：使用者輸入的文字
                    CreatedAt = DateTime.Now // 建立時間：當前伺服器時間
                };
                _context.ChatMessage.Add(userMsg); // 將使用者訊息加入 EF Core 追蹤
                await _context.SaveChangesAsync(); // 非同步保存：將使用者訊息寫入資料庫

                // C. 呼叫 AI 服務 (Gemini) - 這裡現在只會回傳正常的 AI 對話
                string aiResponseText = await _geminiService.GetGeminiReplyAsync(request.UserContent); // 呼叫 Gemini 服務，獲取 AI 文字回覆

                // (原本的 D 步驟已刪除，程式碼直接往下執行)

                // E. 存檔 - AI 回覆
                var aiMsg = new ChatMessage // 建立 AI 回覆訊息實體
                {
                    SessionId = (int)currentSessionId, // 所屬對話 Session ID
                    Role = "assistant", // 角色：標記為 AI 助理回覆的訊息
                    Body = aiResponseText, // 訊息內容：AI 回覆的文字
                    CreatedAt = DateTime.Now // 建立時間：當前伺服器時間
                };
                _context.ChatMessage.Add(aiMsg); // 將 AI 訊息加入 EF Core 追蹤
                await _context.SaveChangesAsync(); // 非同步保存：將 AI 訊息寫入資料庫

                // F. 透過 SignalR 推播文字結果 (讓前端即時顯示)
                if (!string.IsNullOrEmpty(request.ConnectionId)) // 判斷：若前端傳入有效的 SignalR 連接 ID
                {
                    // 透過 SignalR 向指定連接 ID 的用戶端推播 AI 回覆訊息
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "AI", aiResponseText); // 推播方法：ReceiveMessage，參數：發送者、訊息內容
                }

                // G. 回傳 DTO
                var responseDto = new ChatMessageDto // 建立回傳用的 DTO（數據傳輸物件）
                {
                    ChatMessageId = aiMsg.ChatMessageId, // AI 訊息的唯一 ID
                    SessionId = aiMsg.SessionId, // 所屬對話 Session ID
                    Role = aiMsg.Role, // 訊息角色（assistant）
                    Body = aiResponseText, // AI 回覆內容
                    // 建立時間：判斷是否有值，有則格式化為指定字串，無則使用當前時間
                    CreatedAt = aiMsg.CreatedAt.HasValue
                        ? aiMsg.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // 回傳 200 OK 響應，包含成功狀態、Session ID 和 AI 訊息 DTO
                return Ok(new { success = true, sessionId = currentSessionId, data = responseDto });
            }
            catch (Exception ex) // 捕獲所有未處理的例外
            {
                // 回傳 500 伺服器錯誤，包含錯誤訊息
                return StatusCode(500, new { error = $"伺服器錯誤: {ex.Message}" });
            }
        }

        // ==========================================
        // 2. [API] 以圖搜圖 (更新為 UserImage Model)
        // ==========================================
        [HttpPost] // HTTP 方法：響應 POST 請求
        [Route("api/ChatMessages/UploadImage")] // 路由：定義圖片上傳 API 路徑
        [IgnoreAntiforgeryToken] // 忽略防偽令牌驗證
        // 非同步方法：處理使用者上傳圖片請求，參數透過表單表單接收 ImageUploadRequest 物件
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            var imageFile = request.Image; // 取得上傳的圖片檔案
            if (imageFile == null || imageFile.Length == 0) return BadRequest("未上傳圖片"); // 驗證：若無圖片檔案，返回 400 錯誤

            try // 異常處理區塊
            {
                // A. SignalR 通知前端：收到圖片
                if (!string.IsNullOrEmpty(request.ConnectionId)) // 判斷：若有有效的 SignalR 連接 ID
                {
                    // 推播系統訊息：告知前端正在分析圖片
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "System", "正在分析圖片內容，請稍候...");
                }

                // B. 檔案處理 (存到 wwwroot/img/chat)
                // 組合圖片存儲路徑：網站根目錄/wwwroot/img/chat
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img/chat");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder); // 判斷：若目錄不存在，建立該目錄

                string extension = Path.GetExtension(imageFile.FileName); // 取得圖片檔案副檔名（如 .jpg、.png）
                // 使用你指定的命名邏輯：時間戳 + 亂數
                // 生成檔案名：當前時間戳（年月日時分秒） + GUID 前4位 + 副檔名，避免檔名重複
                string shortFileName = $"{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 4)}{extension}";

                // 雙重保險：檢查長度
                if (shortFileName.Length > 50) // 判斷：若檔名長度超過50字元
                {
                    // 重新生成檔名：GUID 前20位 + 副檔名，確保檔名長度合法
                    shortFileName = Guid.NewGuid().ToString().Substring(0, 20) + extension;
                }

                string fullFilePath = Path.Combine(uploadsFolder, shortFileName); // 完整檔案路徑：伺服器本機存儲路徑
                string dbRelativeUrl = $"/img/chat/{shortFileName}"; // 存入 DB 的相對路徑：供前端訪問圖片使用

                // C. 轉換 Base64 (給 AI) 與 寫入硬碟
                string base64Image; // 宣告變數：存儲圖片的 Base64 編碼字串（供 AI 分析）
                using (var memoryStream = new MemoryStream()) // 使用內存流：避免磁碟 IO 操作，提升效率
                {
                    await imageFile.CopyToAsync(memoryStream); // 將上傳的圖片檔案複製到內存流
                    base64Image = Convert.ToBase64String(memoryStream.ToArray()); // 將內存流轉換為 Base64 字串

                    // 重置指標後寫入實體檔案
                    memoryStream.Position = 0; // 重置內存流指標到開頭
                    using (var fileStream = new FileStream(fullFilePath, FileMode.Create)) // 建立檔案流：準備寫入本機檔案
                    {
                        await memoryStream.CopyToAsync(fileStream); // 將內存流內容寫入本機檔案
                    }
                }

                // D. 呼叫 Gemini Vision API
                // 定義提示詞：告訴 AI 分析圖片的規則（電影海報/劇照返回片名，其他返回內容描述）
                string prompt = "這是一張使用者上傳的圖片。如果是電影海報或劇照，請告訴我電影名稱；如果是其他圖片，請描述內容。";
                // 呼叫 Gemini 圖片分析服務：傳入 Base64 圖片、檔案類型、提示詞，獲取 AI 回覆
                string aiReply = await _geminiService.GetGeminiImageReplyAsync(base64Image, imageFile.ContentType, prompt);

                // E. 寫入 UserImage 資料表 (配合你的新 Model)
                var newImage = new UserImage // 建立 UserImage 實體：存儲使用者上傳的圖片資訊
                {
                    UserId = 1, // TODO: 替換為真實 UserID（暫時寫死為1）
                    ImageUrl = dbRelativeUrl, // 圖片訪問路徑：存入相對路徑
                    UsageLocation = 1, // ★ 注意：請確保 ImageUsageLocation 表中有 ID=1 的資料 (例如代表"Chat")
                    Caption = aiReply.Length > 200 ? aiReply.Substring(0, 197) + "..." : aiReply, // 圖片描述：截斷超過200字的內容，避免欄位溢位
                    DisplayOrder = 0, // 顯示順序：預設為0
                    CreatedAt = DateTime.Now // 建立時間：當前伺服器時間
                };
                _context.UserImage.Add(newImage); // 將圖片資訊加入 EF Core 追蹤
                await _context.SaveChangesAsync(); // 非同步保存：寫入 UserImage 資料表

                // F. 寫入 ChatMessage (紀錄對話)
                if (request.SessionId > 0) // 判斷：若有有效的對話 Session ID
                {
                    // 1. 存圖片訊息 (用 HTML 存，方便前端渲染)
                    var userMsg = new ChatMessage // 建立使用者圖片訊息實體
                    {
                        SessionId = request.SessionId, // 所屬對話 Session ID
                        Role = "user", // 角色：使用者
                        // 訊息內容：存儲圖片 HTML 標籤，方便前端直接渲染
                        Body = $"<img src='{dbRelativeUrl}' alt='uploaded image' style='max-width: 200px; border-radius: 8px;' />",
                        CreatedAt = DateTime.Now // 建立時間
                    };
                    _context.ChatMessage.Add(userMsg); // 加入追蹤

                    // 2. 存 AI 辨識結果
                    var aiMsg = new ChatMessage // 建立 AI 圖片分析結果實體
                    {
                        SessionId = request.SessionId, // 所屬對話 Session ID
                        Role = "assistant", // 角色：AI 助理
                        Body = aiReply, // 訊息內容：AI 分析結果
                        CreatedAt = DateTime.Now // 建立時間
                    };
                    _context.ChatMessage.Add(aiMsg); // 加入追蹤
                    await _context.SaveChangesAsync(); // 非同步保存：將圖片訊息和 AI 結果寫入資料庫
                }

                // G. SignalR 推播結果
                if (!string.IsNullOrEmpty(request.ConnectionId)) // 判斷：若有有效的 SignalR 連接 ID
                {
                    // 推播圖片 (前端收到 User_Image 會顯示圖片)
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "User_Image", dbRelativeUrl); // 推播使用者圖片路徑

                    // 推播 AI 文字
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveMessage", "AI", aiReply); // 推播 AI 分析結果
                }

                // 回傳 DTO：返回成功狀態、圖片路徑、AI 回覆
                return Ok(new { success = true, url = dbRelativeUrl, reply = aiReply });
            }
            catch (Exception ex) // 捕獲例外
            {
                // 回傳 500 錯誤，包含例外訊息
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // 3. [API] 取得歷史訊息 (DTO 模式)
        // ==========================================
        [HttpGet] // HTTP 方法：響應 GET 請求
        [Route("api/ChatMessages/GetHistory/{sessionId}")] // 路由：定義獲取歷史訊息的 API 路徑，包含 sessionId 參數
        // 非同步方法：根據 Session ID 獲取該對話的歷史訊息
        public async Task<IActionResult> GetHistory(int sessionId) // sessionId：對話 Session ID（從 URL 路由參數獲取）
        {
            try // 異常處理區塊
            {
                // 查詢指定 Session ID 的所有訊息，並轉換為 DTO
                var history = await _context.ChatMessage // 查詢 ChatMessage 資料表
                    .Where(m => m.SessionId == sessionId) // 過濾條件：只取指定 Session ID 的訊息
                    .OrderBy(m => m.CreatedAt) // 排序：按建立時間升序（從舊到新）
                    .Select(m => new ChatMessageDto // 投影：將查詢結果轉換為 ChatMessageDto
                    {
                        ChatMessageId = m.ChatMessageId, // 訊息唯一 ID
                        SessionId = m.SessionId, // 所屬 Session ID
                        Role = m.Role, // 訊息角色（user/assistant）
                        Body = m.Body, // 訊息內容
                        // ★ 修改這裡：判斷 HasValue
                        // 建立時間：若有值則格式化為指定字串，無則返回空字串
                        CreatedAt = m.CreatedAt.HasValue
                                    ? m.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                    : ""
                    })
                    .ToListAsync(); // 非同步執行查詢，並轉換為 List 集合

                return Ok(history); // 回傳 200 OK，包含歷史訊息列表
            }
            catch (Exception ex) // 捕獲例外
            {
                // 回傳 500 錯誤，包含例外訊息
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}