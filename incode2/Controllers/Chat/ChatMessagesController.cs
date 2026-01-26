using incode2.Models;
using incode2.Dtos; // 修正命名空間 DTO -> Dtos (配合之前的檔案)
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
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Controllers.Chat
{
    public class ChatMessagesController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IConfiguration _configuration;

        public ChatMessagesController(incodedatabaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ==========================================
        // Index 頁面：顯示聊天室列表與對話內容
        // ==========================================
        public async Task<IActionResult> Index(int? id)
        {
            var viewModel = new ChatIndexViewModel();

            // 1. 抓取左側列表 (包含 User 資訊)
            // 注意：這裡 EF Core 會自動撈取 Ishumanmode 欄位
            viewModel.Sessions = await _context.ChatSession
                                           .Include(s => s.User)
                                           .OrderByDescending(s => s.CreatedAt)
                                           .ToListAsync();

            // 2. 決定當前要顯示哪個 Session
            int? targetId = id ?? viewModel.Sessions.FirstOrDefault()?.ChatSessionId;

            // 預設為 AI 模式 (false)，若找不到 Session 則維持 false
            bool isHumanMode = false;

            if (targetId.HasValue)
            {
                viewModel.CurrentChatSessionId = targetId;

                // 找出當前 Session
                var currentSession = viewModel.Sessions.FirstOrDefault(s => s.ChatSessionId == targetId);

                if (currentSession != null)
                {
                    viewModel.TargetUser = currentSession.User;

                    // ★ 關鍵修改：取得當前模式狀態
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

            // ★ 將狀態傳給 View，前端 JS 初始化時會讀取這個變數
            // 用於決定顯示 "真人客服" 按鈕還是 "結束對話" 按鈕
            ViewBag.IsHumanMode = isHumanMode;
            ViewBag.CurrentSessionId = targetId; // 方便前端抓取 ID

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