using incode2.Models; 
using incode2.DTO; 
using incode2.Hubs; 
using incode2.Services; 
using incode2.ViewModels; 
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Mvc.Rendering; 
using Microsoft.AspNetCore.SignalR; 
using Microsoft.EntityFrameworkCore;
using System; 
using System.Collections.Generic; 
using System.IO; 
using incode2.Data; 
using System.Linq; 
using System.Threading.Tasks; 

namespace incode2.Controllers 
{
    // 定義 ChatMessagesController 類別，繼承自 Controller，負責處理網頁請求
    public class ChatMessagesController : Controller
    {
        private readonly incodedatabaseContext _context; // 宣告唯讀的資料庫上下文變數，用於與資料庫溝通
        private readonly IConfiguration _configuration; // 宣告唯讀的組態設定變數，用於讀取 appsettings.json

        // 建構子，透過依賴注入 (DI) 取得資料庫上下文與組態設定
        public ChatMessagesController(incodedatabaseContext context, IConfiguration configuration)
        {
            _context = context; // 將注入的資料庫上下文指派給區域變數
            _configuration = configuration; // 將注入的組態設定指派給區域變數
        }

        // ==========================================
        // Index 頁面：顯示聊天室列表與對話內容 (包含圖片)
        // ==========================================
        // GET: ChatMessages
        // 非同步方法，處理 Index 頁面的 GET 請求，id 參數為選中的 SessionId (可為 null)
        public async Task<IActionResult> Index(int? id)
        {
            var viewModel = new ChatIndexViewModel(); // 初始化一個新的 ViewModel 實例，用於攜帶資料到 View

            // 1. 抓取左側列表 (包含 User 資訊)
            // 從 ChatSession 資料表中撈取資料
            viewModel.Sessions = await _context.ChatSession
                                               .Include(s => s.User) // 透過 Include 預先載入關聯的 User 資料 (為了顯示頭像或名稱)
                                               .OrderByDescending(s => s.CreatedAt) // 依照建立時間降冪排序 (最新的在最上面)
                                               .ToListAsync(); // 非同步執行查詢並將結果轉為 List

            // 2. 決定當前要顯示哪個 Session
            // 如果網址有傳 id，就用 id；如果沒有，就預設使用列表中的第一筆 SessionId
            int? targetId = id ?? viewModel.Sessions.FirstOrDefault()?.ChatSessionId;

            // 如果有找到有效的 SessionId (targetId 有值)
            if (targetId.HasValue)
            {
                viewModel.CurrentChatSessionId = targetId; // 將當前的 SessionId 設定給 ViewModel

                // ★ 新增功能：找出當前這個 Session 對應的 User 物件
                // 從剛剛撈出的 Sessions 列表中，篩選出符合 targetId 的那筆資料
                var currentSession = viewModel.Sessions.FirstOrDefault(s => s.ChatSessionId == targetId);

                // 如果找到了該 Session 資料
                if (currentSession != null)
                {
                    viewModel.TargetUser = currentSession.User; // 將該 Session 的 User 資料放入 ViewModel，讓 View 可以讀取使用者圖片
                }

                // 3. 抓取該 Session 的所有訊息 (包含文字與圖片 HTML)
                // 從 ChatMessage 資料表中查詢
                viewModel.CurrentMessages = await _context.ChatMessage
                                                          .Where(m => m.SessionId == targetId) // 篩選出屬於當前 Session 的訊息
                                                          .OrderBy(m => m.CreatedAt) // 依照訊息時間由舊到新排序
                                                          .ToListAsync(); // 非同步執行查詢並轉為 List
                // 註：這裡撈出的 Body 若包含 <img src="...">，View 端使用 @Html.Raw() 即可顯示圖片
            }
            else
            {
                // 如果完全沒有 Session 資料，則初始化一個空的訊息列表，避免 View 發生 NullReferenceException
                viewModel.CurrentMessages = new List<ChatMessage>();
            }

            return View(viewModel); // 回傳 Index View，並將填好資料的 viewModel 傳遞過去
        }

        // ==========================================
        // Create 頁面：建立新訊息 (保留原功能)
        // ==========================================
        // GET: ChatMessages/Create
        // 顯示建立訊息的表單頁面
        public IActionResult Create()
        {
            // 建立一個 SelectList，包含所有 Session 的 ID 和標題，放入 ViewData 供前端下拉選單使用
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title");
            return View(); // 回傳 Create View
        }

        // POST: ChatMessages/Create
        // 處理建立訊息的 POST 請求
        [HttpPost] // 標記此方法僅接受 POST 請求
        [ValidateAntiForgeryToken] // 驗證防偽權杖，防止 CSRF 攻擊
        public async Task<IActionResult> Create([Bind("ChatMessageId,SessionId,Role,Body,CreatedAt")] ChatMessage chatMessage)
        {
            // 檢查模型驗證狀態 (例如必填欄位是否都有填)
            if (ModelState.IsValid)
            {
                _context.Add(chatMessage); // 將新的 chatMessage 物件加入 Entity Framework 的追蹤
                await _context.SaveChangesAsync(); // 非同步將變更寫入資料庫
                return RedirectToAction(nameof(Index)); // 成功後導向回 Index 頁面
            }
            // 如果驗證失敗，重新準備下拉選單資料
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage); // 回傳 View 並保留原本輸入的資料以便使用者修正
        }

        // ==========================================
        // Edit 頁面：編輯訊息 (保留原功能)
        // ==========================================
        // GET: ChatMessages/Edit/5
        // 顯示編輯訊息的頁面，id 為要編輯的訊息 ID
        public async Task<IActionResult> Edit(long? id)
        {
            // 如果 id 為 null，回傳 NotFound (404)
            if (id == null)
            {
                return NotFound(); // 回傳 404 錯誤結果
            }

            // 根據 id 從資料庫尋找該則訊息
            var chatMessage = await _context.ChatMessage.FindAsync(id);
            // 如果找不到該訊息，回傳 NotFound
            if (chatMessage == null)
            {
                return NotFound(); // 回傳 404 錯誤結果
            }
            // 準備 Session 下拉選單，並預設選中該訊息所屬的 Session
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage); // 回傳 Edit View 並帶入該訊息資料
        }

        // POST: ChatMessages/Edit/5
        // 處理編輯訊息的 POST 請求
        [HttpPost] // 標記此方法僅接受 POST 請求
        [ValidateAntiForgeryToken] // 驗證防偽權杖
        public async Task<IActionResult> Edit(long id, [Bind("ChatMessageId,SessionId,Role,Body,CreatedAt")] ChatMessage chatMessage)
        {
            // 確認網址中的 id 與表單提交的 id 是否一致，不一致則回傳 NotFound
            if (id != chatMessage.ChatMessageId)
            {
                return NotFound(); // 回傳 404 錯誤結果
            }

            // 檢查模型驗證狀態
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chatMessage); // 標記該物件為修改狀態
                    await _context.SaveChangesAsync(); // 非同步將變更寫入資料庫
                }
                catch (DbUpdateConcurrencyException) // 捕捉並行更新例外 (防止多人同時修改同一筆資料的衝突)
                {
                    // 如果該訊息已不存在
                    if (!ChatMessageExists(chatMessage.ChatMessageId))
                    {
                        return NotFound(); // 回傳 404
                    }
                    else
                    {
                        throw; // 如果是其他併發問題，則拋出例外
                    }
                }
                return RedirectToAction(nameof(Index)); // 更新成功後導向回 Index 頁面
            }
            // 如果驗證失敗，重新準備下拉選單
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage); // 回傳 View 保留輸入資料
        }

        // ==========================================
        // Delete 頁面：刪除訊息 (保留原功能)
        // ==========================================
        // GET: ChatMessages/Delete/5
        // 顯示刪除確認頁面
        public async Task<IActionResult> Delete(long? id)
        {
            // 如果 id 為 null，回傳 NotFound
            if (id == null)
            {
                return NotFound(); // 回傳 404
            }

            // 查詢要刪除的訊息，並包含關聯的 Session 資料 (為了顯示是哪個聊天室的訊息)
            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session) // 預先載入 Session 關聯資料
                .FirstOrDefaultAsync(m => m.ChatMessageId == id); // 尋找第一筆符合 ID 的資料

            // 如果找不到該訊息
            if (chatMessage == null)
            {
                return NotFound(); // 回傳 404
            }

            return View(chatMessage); // 回傳 Delete View 供使用者確認
        }

        // POST: ChatMessages/Delete/5
        // 處理確認刪除的 POST 請求
        [HttpPost, ActionName("Delete")] // 雖然方法名叫 DeleteConfirmed，但對應的 Action 名稱是 Delete
        [ValidateAntiForgeryToken] // 驗證防偽權杖
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            // 根據 ID 尋找要刪除的訊息
            var chatMessage = await _context.ChatMessage.FindAsync(id);
            // 如果找到了該訊息
            if (chatMessage != null)
            {
                _context.ChatMessage.Remove(chatMessage); // 將該訊息標記為刪除
            }

            await _context.SaveChangesAsync(); // 執行資料庫刪除操作
            return RedirectToAction(nameof(Index)); // 刪除後導向回 Index 頁面
        }

        // 私有輔助方法：檢查訊息是否存在
        private bool ChatMessageExists(long id)
        {
            // 檢查資料庫中是否有任何一筆資料的 ChatMessageId 等於傳入的 id
            return _context.ChatMessage.Any(e => e.ChatMessageId == id);
        }
    }
}