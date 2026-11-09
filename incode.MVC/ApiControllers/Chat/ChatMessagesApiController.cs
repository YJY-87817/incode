using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace incode.MVC.ApiControllers.Chat
{
    [RequireHttps]
    [Authorize]
    public class ChatMessagesApiController : Controller
    {
        private const long MaxChatImageBytes = 10L * 1024 * 1024; // 聊天圖片單檔 10 MB

        private readonly IChatService _chat;
        private readonly GeminiService _geminiService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _frontendUrl; // 前端網址 (App:FrontendUrl)，AI 回覆中產生連結用
        private readonly ILogger<ChatMessagesApiController> _logger;
        private readonly UploadStorage _storage;

        public ChatMessagesApiController(
          IChatService chat,
          GeminiService geminiService,
          IHubContext<ChatHub> hubContext,
          IWebHostEnvironment webHostEnvironment,
          IConfiguration configuration,
          ILogger<ChatMessagesApiController> logger,
          UploadStorage storage)
        {
            _logger = logger;
            _storage = storage;
            _chat = chat;
            _geminiService = geminiService;
            _hubContext = hubContext;
            _webHostEnvironment = webHostEnvironment;
            _frontendUrl = configuration.GetFrontendUrl();
        }


        // 非預期錯誤：記錄例外，回傳通用訊息（不要把 ex.Message 回給前端）
        private IActionResult ServerError(Exception ex, string action)
        {
            _logger.LogError(ex, "{Action}失敗", action);
            return StatusCode(500, new { message = $"{action}失敗，請稍後再試", error = $"{action}失敗" });
        }

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

        // Function Calling 的資料庫查詢方法 (商品)
        private string GetProductInfo(string keyword)
        {
            // 簡單模擬：如果關鍵字包含以下詞彙，就回傳該商品資料
            if (keyword.Contains("電影") || keyword.Contains("票") || keyword.Contains("兌換"))
            {
                var productData = new
                {
                    productId = 1,
                    name = "電影兌換卷*1",
                    description = "全台通用電影兌換券，不限平假日皆可使用。適用於首輪電影，提供最震撼的感官體驗，是下班休閒或週末約會的最佳選擇。",
                    imageName = "2026010611432290af.jpg",
                    type = "兌換票券",
                    point = 2000
                };
                return JsonSerializer.Serialize(productData);
            }
            return "查無相關商品資料。";
        }

        // Function Calling 的資料庫查詢方法 (文章) 
        private string SearchContents(string keyword)
        {
            // 如果關鍵字包含以下詞彙，就回傳文章資料
            if (keyword.Contains("寄生上流") || keyword.Contains("亞洲") || keyword.Contains("電影"))
            {
                var contentData = new
                {
                    contentId = 3,
                    title = "寄生上流這部片",
                    categoryName = "亞洲電影",
                    username = "吳建豪",
                    body = "/contents/content-3.html",
                };
                return JsonSerializer.Serialize(contentData);
            }
            return "查無相關文章。";
        }

        // Function Calling 的資料庫查詢方法 (點數)
        private string PointLogs(string keyword)
        {
            // 如果關鍵字包含以下詞彙，就回傳點數資料
            if (keyword.Contains("點數") || keyword.Contains("紀錄") || keyword.Contains("剩餘"))
            {
                var pointData = new
                {
                    pointLogId = 2,
                    userId = 1,
                    sourceType = "entrust_mission_point_reserve",
                    sourceId = 1,
                    pointTypeDefinitionId = 0,
                    changeAmount = -500,
                    balanceAfter = 1500,
                    displayName = "委託發布預扣"
                };
                return JsonSerializer.Serialize(pointData);
            }
            return "查無相關點數。";
        }


        // 取得或建立唯一對話
        [HttpPost]
        [Route("api/ChatMessages/GetOrCreateSession")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetOrCreateSession()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("請先登入");

            try
            {
                // 自動找舊的，沒有就開新的
                var result = await _chat.GetOrInitSessionAsync(userId, 0);

                return Ok(new { sessionId = result.Session.ChatSessionId, isNew = result.IsNew });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立客服對話失敗");
                return StatusCode(500, new { message = "建立對話失敗，請稍後再試", error = "建立對話失敗" });
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
                // Session 處理
                var (currentSession, _) = await _chat.GetOrInitSessionAsync(userId, request.SessionId,
                    request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent);

                if (currentSession == null) return Unauthorized("您無權存取此對話");

                string sessionIdStr = currentSession.ChatSessionId.ToString();
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);

                // 存 User 訊息
                await _chat.AddMessageAsync(currentSession.ChatSessionId, "user", request.UserContent);

                var sessionGroup = _hubContext.Clients.Group(sessionIdStr);
                await sessionGroup.SendAsync("ReceiveMessage", "User", request.UserContent);

                // 真人客服模式攔截
                if (currentSession.Ishumanmode == true)
                {
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("ReceiveSupportRequest", currentSession.ChatSessionId, request.UserContent);
                    return Ok(new { success = true, mode = "live", sessionId = currentSession.ChatSessionId });
                }

                // AI 處理邏輯
                string aiResponseText = "";
                string messageType = "AI";

                // 生圖指令
                if (request.UserContent.Contains("生圖") || request.UserContent.Contains("產生圖片"))
                {
                    string imageDescription = request.UserContent.Replace("生圖", "").Replace("產生圖片", "").Trim();
                    aiResponseText = await _geminiService.GenerateImageAsync(imageDescription);
                    messageType = "AI_Image";
                }
                else
                {
                    // 一般對話 (包含工具判斷)

                    // 從 Prompts.md 讀取系統提示詞
                    string basePrompt = await GetSystemPromptAsync();

                    // 第一次呼叫 AI (意圖判斷)
                    string toolSystemPrompt = $@"{basePrompt}
                    
                    使用者問題: {request.UserContent}";

                    string firstResponse = await _geminiService.GetGeminiReplyAsync(toolSystemPrompt);

                    // 檢查 AI 是否回傳了呼叫指令
                    if (firstResponse.Contains("CALL_FUNCTION:GetProductInfo"))
                    {
                        // --- 呼叫商品工具 ---
                        string keyword = firstResponse.Split('|')[1].Trim();
                        string productDataJson = GetProductInfo(keyword);
                        string imagePathPrefix = "/img/product/";
                        string frontendUrl = _frontendUrl;

                        string finalPrompt = $@"{basePrompt}
                        ---
                        你剛剛針對關鍵字「{keyword}」查詢了商品資料庫。
                        【查詢結果 JSON】:
                        {productDataJson}

                        請根據上述 JSON 資料，親切地回答使用者的問題 (包含商品點數)。
                        
                        【圖片顯示強制規則】：
                        資料中包含 'imageName' 欄位 (例如 'xxx.jpg')。
                        請務必在回答的最後面，換行並插入一張圖片，使用 Markdown 語法：
                        ![商品圖片]({imagePathPrefix}<這裡填入 imageName>)
                        (注意：請直接將 JSON 中的 imageName 填入，不要自行捏造網址)
                        【連結顯示規則】：
                        請使用 Markdown 格式產生連結：[商品標題]({frontendUrl}/PointShop/product/{{productId}})
                        例如：[電影票卷]({frontendUrl}PointShop/product/1)
                        ---
                        使用者問題: {request.UserContent}";

                        aiResponseText = await _geminiService.GetGeminiReplyAsync(finalPrompt);
                    }
                    else if (firstResponse.Contains("CALL_FUNCTION:SearchContents"))
                    {
                        // --- 呼叫文章工具 ---
                        string keyword = firstResponse.Split('|')[1].Trim();
                        string contentDataJson = SearchContents(keyword);
                        string frontendUrl = _frontendUrl;

                        string finalPrompt = $@"{basePrompt}
                        ---
                        你剛剛針對關鍵字「{keyword}」查詢了文章資料庫。
                        【查詢結果 JSON】:
                        {contentDataJson}

                        請根據上述 JSON 資料回答，並務必提供文章連結。
                        【連結顯示規則】：
                        請使用 Markdown 格式產生連結：[文章標題]({frontendUrl}/forum/{{contentId}})
                        例如：[寄生上流這部片]({frontendUrl}/forum/3)
                        ---
                        使用者問題: {request.UserContent}";

                        aiResponseText = await _geminiService.GetGeminiReplyAsync(finalPrompt);
                    }
                    else if (firstResponse.Contains("CALL_FUNCTION:PointLogs"))
                    {
                        // --- 呼叫點數工具 ---
                        string keyword = firstResponse.Split('|')[1].Trim();
                        string pointDataJson = PointLogs(keyword);
                        string frontendUrl = _frontendUrl;

                        string finalPrompt = $@"{basePrompt}
                        ---
                        你剛剛針對關鍵字「{keyword}」查詢了點數資料庫。
                        【查詢結果 JSON】:
                        {pointDataJson}

                        請根據上述 JSON 資料回答，並務必提供點數連結。
                        【連結顯示規則】：
                        請使用 Markdown 格式產生連結：[查看點數紀錄]({frontendUrl}/point-log)
                        (即使 JSON 中有 pointLogId，也不要把它加到網址裡，因為該頁面是列表頁)
                        ---
                        使用者問題: {request.UserContent}";

                        aiResponseText = await _geminiService.GetGeminiReplyAsync(finalPrompt);
                    }
                    else
                    {
                        // --- 沒有點數工具，直接回覆 ---
                        aiResponseText = firstResponse;
                    }
                }

                // 存 AI 訊息 (您可能漏了這一段)
                await _chat.AddMessageAsync(currentSession.ChatSessionId, "assistant", aiResponseText);

                // 廣播 AI 回應
                await sessionGroup.SendAsync("ReceiveMessage", messageType, aiResponseText);

                // 關鍵：這裡必須要回傳 Ok，否則就會報錯
                return Ok(new { success = true, sessionId = currentSession.ChatSessionId, mode = "ai" });
            }
            catch (Exception ex)
            {
                // Catch 區塊也必須有回傳值
                _logger.LogError(ex, "客服聊天處理失敗");
                return StatusCode(500, new { message = "伺服器錯誤，請稍後再試", error = "伺服器錯誤" });
            }
        }

        [HttpGet]
        [Route("api/ChatMessages/GetActiveSupportSessions")]
        [Authorize(Roles = "Admin")] // 客服專用：會列出所有會員的對話
        public async Task<IActionResult> GetActiveSupportSessions()
        {

            try
            {
                // 客服不需要自動建立 Session，而是撈取已經存在的、且 Ishumanmode 為 true 的 Session
                var sessions = await _chat.GetSupportSessionsAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return ServerError(ex, "取得客服對話列表");
            }
        }

        // 客服回覆
        [HttpPost]
        [Route("api/ChatMessages/AdminReply")]
        [Authorize(Roles = "Admin")] // 客服專用：以客服身分回覆任何對話
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AdminReply([FromBody] MessageRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.SessionId <= 0) return BadRequest("無效的 Session ID");

            try
            {
                string sessionIdStr = request.SessionId.ToString();
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);

                // 客服回覆存檔 
                // 客服與 AI 統一使用 assistant 角色
                await _chat.AddMessageAsync(request.SessionId, "assistant", request.UserContent);

                await _hubContext.Clients.Group(sessionIdStr)
                  .SendAsync("ReceiveMessage", "Live", request.UserContent);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "客服回覆失敗");
                return StatusCode(500, new { message = "客服回覆失敗，請稍後再試", error = "客服回覆失敗" });
            }
        }

        // 切換模式 (呼叫客服)
        [HttpPost]
        [Route("api/ChatMessages/SwitchMode")]
        [IgnoreAntiforgeryToken] // 因為採用前後端分離 (Vue + Axios)，前端不會自動攜帶傳統 MVC 的防偽造權杖(CSRF Token)。
                                 // 為了避免 API 請求被安全機制誤擋，在此加上[IgnoreAntiforgeryToken]予以豁免。
        public async Task<IActionResult> SwitchMode([FromBody] SwitchModeDto request)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("請先登入");

            try
            {
                var session = await _chat.FindSessionAsync(request.SessionId);

                // Session 處理與權限檢查
                if (session == null)
                {
                    var result = await _chat.GetOrInitSessionAsync(userId, request.SessionId);
                    session = result.Session!;
                }
                else if (session.UserId != userId)
                {
                    return Unauthorized();
                }

                // 更新模式與時間
                await _chat.SetHumanModeAsync(session, request.Ishumanmode);

                string modeName = request.Ishumanmode ? "客服" : "AI";
                string sessionIdStr = session.ChatSessionId.ToString();

                // SignalR 通知
                await EnsureGroupAsync(request.ConnectionId, sessionIdStr);
                var sessionGroup = _hubContext.Clients.Group(sessionIdStr);

                await sessionGroup.SendAsync("ReceiveMessage", "System", $"已切換至 {modeName} 模式");
                await sessionGroup.SendAsync("UpdateModeState", session.ChatSessionId, request.Ishumanmode);

                // 若切換為真人，處理存檔與通知
                if (request.Ishumanmode)
                {
                    string autoReplyText = "收到您的請求，請稍等1-2分鐘，客服人員將為您服務。";

                    await _chat.AddMessageAsync(session.ChatSessionId, "assistant", autoReplyText);

                    await sessionGroup.SendAsync("ReceiveMessage", "Live", autoReplyText);
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("RefreshSessions");
                }

                // 你指定的唯一回傳行
                return Ok(new { success = true, mode = modeName, sessionId = session.ChatSessionId });
            }
            catch (Exception ex)
            {
                // 確保 catch 也有回傳，避免編譯錯誤
                return ServerError(ex, "切換客服模式");
            }
        }



        [HttpPost]
        [Route("api/ChatMessages/UploadImage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();
            if (request.Image == null || request.Image.Length == 0) return BadRequest("未上傳圖片");

            // 只接受圖片（副檔名 + 檔頭比對），避免把 .html / .svg 等檔案寫進 wwwroot
            var imageError = ImageFileValidator.Validate(request.Image, MaxChatImageBytes);
            if (imageError != null) return BadRequest(imageError);

            var (currentSession, isNew) = await _chat.GetOrInitSessionAsync(userId, request.SessionId, "圖片對話");
            if (currentSession == null) return Unauthorized("您無權存取此對話");
            int validSessionId = currentSession.ChatSessionId;
            string validSessionIdStr = validSessionId.ToString();

            try
            {
                // 圖片實體存檔（UploadStorage，網站目錄外）
                // 檔名只用 Guid + 正規化副檔名，不使用使用者提供的檔名（原本可用 ..\ 寫到其他目錄）
                string uniqueFileName = $"{Guid.NewGuid()}{ImageFileValidator.NormalizedExtension(request.Image)}";
                string dbRelativeUrl = await _storage.SaveAsync(UploadFolders.ChatImages, uniqueFileName, request.Image);

                // 存入 User 圖片訊息
                await EnsureGroupAsync(request.ConnectionId, validSessionIdStr);
                await _chat.AddMessageAsync(validSessionId, "user",
                    $"<img src='{dbRelativeUrl}' /> {System.Net.WebUtility.HtmlEncode(request.UserContent)}");

                // 推播圖片給前端
                await _hubContext.Clients.Group(validSessionIdStr).SendAsync("ReceiveMessage", "User_Image", dbRelativeUrl);

                // 2. 判斷模式
                if (currentSession.Ishumanmode)
                {
                    // 真人模式：通知客服
                    await _hubContext.Clients.Group("SupportAgents").SendAsync("ReceiveSupportRequest", validSessionId, "(使用者傳送了圖片)");
                    return Ok(new { success = true, sessionId = validSessionId, isNew = isNew });
                }


                // AI 模式：圖片 + Function Calling 

                string aiReplyText = "";
                string basePrompt = await GetSystemPromptAsync();
                string userText = string.IsNullOrWhiteSpace(request.UserContent) ? "這張圖是什麼？" : request.UserContent;

                // Step 1: 第一次呼叫 (看圖 + 判斷是否需要查資料) (★ 修改處：加入文章查詢規則)
                string toolSystemPrompt = $@"{basePrompt}
                ---
                【圖片分析與工具使用指示】
                使用者上傳了一張圖片並附帶文字：「{userText}」。
                
                請辨識圖片內容，並判斷使用者的意圖：

                1. **[查商品]**
                   - 觸發條件：圖片看起來像「商品」、「電影票」、「實體票券」或「兌換券」時，請一律查詢商品資訊。
                   - 回傳格式：CALL_FUNCTION:GetProductInfo|{{商品名稱}}
                
                2. **[查文章]**
                   - 觸發條件：圖片是「電影海報」、「劇照」，且使用者詢問「影評」、「連結」、「介紹」、「好不好看」。
                   - 回傳格式：CALL_FUNCTION:SearchContents|{{電影名稱}}
                
                3. **[純閒聊/描述]**
                   - 觸發條件：不符合上述條件的普通圖片。
                   - 回傳格式：直接回答使用者。

                範例：
                (圖是電影票) -> 回傳: CALL_FUNCTION:GetProductInfo|電影兌換卷
                (圖是寄生上流海報, 問影評) -> 回傳: CALL_FUNCTION:SearchContents|寄生上流
                (圖是風景照) -> 直接回答: 這是一張美麗的風景照。
                ---
                使用者文字: {userText}";

                // 呼叫新寫的 Service 方法 (傳入圖片 + Prompt)
                string firstResponse = await _geminiService.AnalyzeImageWithToolAsync(request.Image, toolSystemPrompt);

                // Step 2: 檢查是否觸發 Function Calling (SearchContents 判斷)
                if (firstResponse.Contains("CALL_FUNCTION:GetProductInfo"))
                {
                    // A. 解析關鍵字
                    string keyword = firstResponse.Split('|')[1].Trim();

                    // B. 查資料庫 (模擬)
                    string productDataJson = GetProductInfo(keyword);
                    string imagePathPrefix = "/img/product/";

                    // C. 帶著資料 + 圖片 + 原始問題，再次問 AI
                    string finalPrompt = $@"{basePrompt}
                    ---
                    你剛剛辨識出圖片中的商品為「{keyword}」，並查詢了資料庫。
                    
                    【查詢結果 JSON】:
                    {productDataJson}

                    請根據圖片內容以及上述 JSON 資料，回答使用者的問題。
                    如果資料中有 'imageName'，請在最後插入圖片：![商品圖片]({imagePathPrefix}{{imageName}})
                    ---
                    使用者文字: {userText}";

                    // 這裡第二次呼叫也建議把圖片帶進去，讓 AI 可以對照圖片細節與資料庫描述
                    aiReplyText = await _geminiService.AnalyzeImageWithToolAsync(request.Image, finalPrompt);
                }
                else if (firstResponse.Contains("CALL_FUNCTION:SearchContents")) // 處理文章查詢
                {
                    string keyword = firstResponse.Split('|')[1].Trim();
                    string contentDataJson = SearchContents(keyword);
                    string frontendUrl = _frontendUrl;

                    string finalPrompt = $@"{basePrompt}
                    ---
                    你剛剛辨識出圖片中的電影為「{keyword}」，並查詢了文章資料庫。
                    
                    【查詢結果 JSON】:
                    {contentDataJson}

                    請根據 JSON 資料回答，並提供文章連結：[文章標題]({frontendUrl}/forum/{{contentId}})
                    ---
                    使用者文字: {userText}";

                    aiReplyText = await _geminiService.AnalyzeImageWithToolAsync(request.Image, finalPrompt);
                }
                else
                {
                    // 沒觸發工具，直接用 AI 的看圖回答
                    aiReplyText = firstResponse;
                }

                // 存入 AI 回覆
                await _chat.AddMessageAsync(validSessionId, "assistant", aiReplyText);

                // 推播 AI 回覆
                await _hubContext.Clients.Group(validSessionIdStr).SendAsync("ReceiveMessage", "AI", aiReplyText);

                return Ok(new { success = true, sessionId = validSessionId, isNew = isNew });
            }
            catch (Exception ex) { return ServerError(ex, "上傳聊天圖片"); }
        }

        // 取得歷史訊息
        [HttpGet]
        [Route("api/ChatMessages/GetHistory/{sessionId}")]
        public async Task<IActionResult> GetHistory(int sessionId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // 只能讀自己的對話 (客服 / 管理員可讀全部)
            if (!User.IsInRole("Admin") &&
                !await _chat.IsOwnerAsync(sessionId, userId))
                return NotFound("找不到該對話");

            try
            {
                var history = await _chat.GetHistoryAsync(sessionId);
                return Ok(history);
            }
            catch (Exception ex) { return ServerError(ex, "取得聊天紀錄"); }
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