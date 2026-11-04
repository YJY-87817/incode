using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode.MVC.Controllers.Chat
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class ChatMessagesController : Controller
    {
        private readonly incodedatabaseContext _context;

        public ChatMessagesController(incodedatabaseContext context)
        {
            _context = context;
        }

        // ==========================================
        // Index 頁面：顯示聊天室列表與對話內容
        // ==========================================
        public async Task<IActionResult> Index(int? id)
        {
            var viewModel = new ChatIndexViewModel();

            // 1. 抓取左側列表 (包含 User 資訊)
            // ★ 關鍵：Include(s => s.User) 讓 View 可以讀取 s.User.Username
            viewModel.Sessions = await _context.ChatSession
                .Include(s => s.User)
                .OrderByDescending(s => s.UpdatedAt) // 通常按更新時間排序比較好
                .ToListAsync();

            // 2. 決定當前要顯示哪個 Session
            // 如果網址有 id 就用 id，沒有就預設抓第一筆
            int? targetId = id ?? viewModel.Sessions.FirstOrDefault()?.ChatSessionId;

            bool isHumanMode = false;

            if (targetId.HasValue)
            {
                viewModel.CurrentChatSessionId = targetId;

                // 找出當前 Session
                var currentSession = viewModel.Sessions.FirstOrDefault(s => s.ChatSessionId == targetId);

                if (currentSession != null)
                {
                    // ★ 設定 TargetUser，讓 View 可以顯示「正在與 XXX 對話」
                    viewModel.TargetUser = currentSession.User;

                    // 讀取當前模式狀態
                    isHumanMode = currentSession.Ishumanmode;
                }

                // 3. 抓取該 Session 的所有訊息
                viewModel.CurrentMessages = await _context.ChatMessage
                    .Where(m => m.SessionId == targetId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                viewModel.CurrentMessages = new List<ChatMessage>();
            }

            // ★ 將狀態傳給 View
            ViewBag.IsHumanMode = isHumanMode;
            ViewBag.CurrentSessionId = targetId;

            return View(viewModel);
        }

        // ==========================================
        // 以下 CRUD 方法保持不變，僅做格式整理
        // ==========================================

        public IActionResult Create()
        {
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ChatMessageId,SessionId,Role,Body,CreatedAt")] ChatMessage chatMessage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chatMessage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var chatMessage = await _context.ChatMessage.FindAsync(id);
            if (chatMessage == null) return NotFound();

            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ChatMessageId,SessionId,Role,Body,CreatedAt")] ChatMessage chatMessage)
        {
            if (id != chatMessage.ChatMessageId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chatMessage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChatMessageExists(chatMessage.ChatMessageId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage);
        }

        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session)
                .FirstOrDefaultAsync(m => m.ChatMessageId == id);

            if (chatMessage == null) return NotFound();

            return View(chatMessage);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var chatMessage = await _context.ChatMessage.FindAsync(id);
            if (chatMessage != null)
            {
                _context.ChatMessage.Remove(chatMessage);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChatMessageExists(long id)
        {
            return _context.ChatMessage.Any(e => e.ChatMessageId == id);
        }
    }
}