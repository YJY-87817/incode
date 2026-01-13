using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using incode2.Models;

namespace incode2.Controllers
{
    // 1. 強制 HTTPS (SSL)
    [RequireHttps]
    public class ChatSessionsController : Controller
    {
        private readonly incodedatabaseContext _context;

        public ChatSessionsController(incodedatabaseContext context)
        {
            _context = context;
        }

        // ==========================================
        // Part 1: MVC 後台管理畫面 (給管理員用)
        // ==========================================

        // GET: ChatSessions
        public async Task<IActionResult> Index()
        {
            // 優化變數名稱，並依照建立時間降序排列 (新的在上面)
            var sessions = _context.ChatSession
                                   .Include(c => c.User)
                                   .OrderByDescending(s => s.CreatedAt);
            return View(await sessions.ToListAsync());
        }

        // GET: ChatSessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var chatSession = await _context.ChatSession
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ChatSessionId == id);

            if (chatSession == null) return NotFound();

            return View(chatSession);
        }

        // GET: ChatSessions/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account");
            return View();
        }

        // POST: ChatSessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ChatSessionId,UserId,Title,CreatedAt,UpdatedAt")] ChatSession chatSession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chatSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId);
            return View(chatSession);
        }

        // GET: ChatSessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var chatSession = await _context.ChatSession.FindAsync(id);
            if (chatSession == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId);
            return View(chatSession);
        }

        // POST: ChatSessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ChatSessionId,UserId,Title,CreatedAt,UpdatedAt")] ChatSession chatSession)
        {
            if (id != chatSession.ChatSessionId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chatSession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChatSessionExists(chatSession.ChatSessionId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.User, "UserId", "Account", chatSession.UserId);
            return View(chatSession);
        }

        // GET: ChatSessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var chatSession = await _context.ChatSession
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ChatSessionId == id);

            if (chatSession == null) return NotFound();

            return View(chatSession);
        }

        // POST: ChatSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chatSession = await _context.ChatSession.FindAsync(id);
            if (chatSession != null)
            {
                _context.ChatSession.Remove(chatSession);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // Part 2: Vue 前端專用 API (JSON)
        // ==========================================

        // [API] 取得該使用者的對話列表
        [HttpGet("api/ChatSessions/User/{userId}")]
        public async Task<IActionResult> GetSessionsApi(int userId)
        {
            var sessions = await _context.ChatSession
                                         .Where(s => s.UserId == userId)
                                         .OrderByDescending(s => s.UpdatedAt) // 最近更新的在最上面
                                         .Select(s => new {
                                             s.ChatSessionId,
                                             s.Title,
                                             s.UpdatedAt
                                         })
                                         .ToListAsync();
            return Ok(sessions);
        }

        // [API] 建立新對話
        [HttpPost("api/ChatSessions/Create")]
        public async Task<IActionResult> CreateSessionApi([FromBody] ChatSession session)
        {
            if (session == null) return BadRequest("無效的資料");

            session.CreatedAt = DateTime.Now;
            session.UpdatedAt = DateTime.Now;

            if (string.IsNullOrEmpty(session.Title)) session.Title = "新對話";

            // 注意：實際上這裡應該驗證 UserId 是否有效
            _context.ChatSession.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new { sessionId = session.ChatSessionId, title = session.Title });
        }

        // [API] 更新標題 (通常 AI 總結第一句話後會呼叫這個)
        [HttpPut("api/ChatSessions/UpdateTitle/{id}")]
        public async Task<IActionResult> UpdateSessionTitleApi(int id, [FromBody] ChatSession updateData)
        {
            var session = await _context.ChatSession.FindAsync(id);
            if (session == null) return NotFound();

            session.Title = updateData.Title;
            session.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "標題更新成功" });
        }

        // 2. 新增 [API] 刪除對話 (給 Vue 前端垃圾桶按鈕用)
        [HttpDelete("api/ChatSessions/Delete/{id}")]
        public async Task<IActionResult> DeleteSessionApi(int id)
        {
            var session = await _context.ChatSession.FindAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "找不到該對話紀錄" });
            }

            // 同時刪除該 Session 底下的所有 Messages (如果有設定 Cascade Delete 資料庫會自動處理，若無則需手動)
            var messages = _context.ChatMessage.Where(m => m.SessionId == id);
            _context.ChatMessage.RemoveRange(messages);

            _context.ChatSession.Remove(session);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "刪除成功" });
        }

        private bool ChatSessionExists(int id)
        {
            return _context.ChatSession.Any(e => e.ChatSessionId == id);
        }
    }
}