using incode2.Data;
using incode2.Models;
using incode2.Services; // 引用 Service
using incode2.DTO;      // 引用 DTO
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace incode2.Controllers
{
    [RequireHttps]
    public class ChatMessagesController : Controller
    {
        
        private readonly incodedatabaseContext _context; //資料庫Context 

        
        private readonly GeminiService _geminiService; // Gemini Service (api)

        
        public ChatMessagesController(incodedatabaseContext context, GeminiService geminiService)
        {
            _context = context;         // CRUD用
            _geminiService = geminiService; // AI聊天用
        }


        // GET: ChatMessages (後台顯示所有對話紀錄)
        public async Task<IActionResult> Index()
        {
            var messages = _context.ChatMessage.Include(c => c.Session).OrderByDescending(c => c.CreatedAt);
            return View(await messages.ToListAsync());
        }

        // GET: ChatMessages/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session)
                .FirstOrDefaultAsync(m => m.ChatMessageId == id);

            if (chatMessage == null) return NotFound();

            return View(chatMessage);
        }

        // GET: ChatMessages/Delete/5 (管理員刪除功能)
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();

            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session)
                .FirstOrDefaultAsync(m => m.ChatMessageId == id);

            if (chatMessage == null) return NotFound();

            return View(chatMessage);
        }

        // POST: ChatMessages/Delete/5
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


        // [API] 接收 Vue 前端傳來的訊息
        [HttpPost]
        [Route("api/ChatMessages/SendMessage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 處理 Session
                long currentSessionId = request.SessionId;
                if (currentSessionId <= 0)
                {
                    var newSession = new ChatSession
                    {
                        Title = request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent,
                        CreatedAt = DateTime.Now,
                        UserId = 1 
                    };
                    _context.ChatSession.Add(newSession);
                    await _context.SaveChangesAsync();
                    currentSessionId = newSession.ChatSessionId;
                }

                // 2. 【存檔-使用者】
                var userMsg = new ChatMessage
                {
                    SessionId = (int)currentSessionId,
                    Role = "user",
                    Body = request.UserContent,
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(userMsg);
                await _context.SaveChangesAsync();

                // ==========================================
                // 3. 呼叫我們辛苦寫好的 Service！
                // ==========================================
                string aiResponseText = await _geminiService.GetGeminiReplyAsync(request.UserContent);

                // 4. 【存檔-AI】
                var aiMsg = new ChatMessage
                {
                    SessionId = (int)currentSessionId,
                    Role = "assistant",
                    Body = aiResponseText,
                    CreatedAt = DateTime.Now
                };
                _context.ChatMessage.Add(aiMsg);
                await _context.SaveChangesAsync();

                // 5. 回傳結果
                return Ok(new
                {
                    success = true,
                    sessionId = currentSessionId,
                    reply = aiResponseText
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"伺服器錯誤: {ex.Message}" });
            }
        }
    }
}