using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Controllers.Chat
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    [RequireHttps]
    public class ChatSessionsController : Controller
    {
        private readonly incodedatabaseContext _context;

        public ChatSessionsController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: ChatSessions
        public async Task<IActionResult> Index()
        {
            var sessions = _context.ChatSession
                .Include(c => c.User)
                .OrderByDescending(s => s.CreatedAt); // 保持降序，方便管理
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
        // ★ 修改：加入 Ishumanmode 到 Bind 列表
        public async Task<IActionResult> Create([Bind("ChatSessionId,UserId,Title,Ishumanmode,CreatedAt,UpdatedAt")] ChatSession chatSession)
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
        // ★ 修改：加入 Ishumanmode 到 Bind 列表
        public async Task<IActionResult> Edit(int id, [Bind("ChatSessionId,UserId,Title,Ishumanmode,CreatedAt,UpdatedAt")] ChatSession chatSession)
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

        private bool ChatSessionExists(int id)
        {
            return _context.ChatSession.Any(e => e.ChatSessionId == id);
        }
    }
}