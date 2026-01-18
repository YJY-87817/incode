using Azure.Core;
using Google.GenAI;
using Google.GenAI.Types;

using incode2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;


namespace incode2.Controllers
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

        // GET: ChatMessages
        public async Task<IActionResult> Index()
        {
            var incodedatabaseContext = _context.ChatMessage.Include(c => c.Session);
            return View(await incodedatabaseContext.ToListAsync());
        }

        // GET: ChatMessages/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session)
                .FirstOrDefaultAsync(m => m.ChatMessageId == id);
            if (chatMessage == null)
            {
                return NotFound();
            }

            return View(chatMessage);
        }

        // GET: ChatMessages/Create
        public IActionResult Create()
        {
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title");
            return View();
        }

        // POST: ChatMessages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: ChatMessages/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatMessage = await _context.ChatMessage.FindAsync(id);
            if (chatMessage == null)
            {
                return NotFound();
            }
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage);
        }

        // POST: ChatMessages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ChatMessageId,SessionId,Role,Body,CreatedAt")] ChatMessage chatMessage)
        {
            if (id != chatMessage.ChatMessageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chatMessage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChatMessageExists(chatMessage.ChatMessageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SessionId"] = new SelectList(_context.ChatSession, "ChatSessionId", "Title", chatMessage.SessionId);
            return View(chatMessage);
        }

        // GET: ChatMessages/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatMessage = await _context.ChatMessage
                .Include(c => c.Session)
                .FirstOrDefaultAsync(m => m.ChatMessageId == id);
            if (chatMessage == null)
            {
                return NotFound();
            }

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

        private bool ChatMessageExists(long id)
        {
            return _context.ChatMessage.Any(e => e.ChatMessageId == id);
        }

        [HttpGet("api/ChatMessages/Session/{sessionId}")]
        public async Task<IActionResult> GetMessagesApi(int sessionId)
        {
            var messages = await _context.ChatMessage
                                         .Where(m => m.SessionId == sessionId)
                                         .OrderBy(m => m.CreatedAt)
                                         .Select(m => new { m.Role, m.Body, m.CreatedAt })
                                         .ToListAsync();
            return Ok(messages);
        }


        public IActionResult RealTimeChat()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {

            // 檢查是否有收到資料
            if (request == null || string.IsNullOrEmpty(request.UserContent))
            {
                return BadRequest("內容不能為空");
            }

            string apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return BadRequest("API Key 未設定");

            try
            {
                var client = new Client(apiKey: apiKey);
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: request.UserContent // 改用 request.UserContent
                );

                string responseText = response.Candidates?[0].Content.Parts[0].Text ?? "AI 無回應";

                // 建議回傳 JSON 格式，前端比較好處理
                return Ok(new { reply = responseText });
            }
            catch (Exception ex)
            {
                return BadRequest($"AI Error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCountryInfo(string countryName = "Australia")
        {
            string apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return BadRequest("API Key 未設定");

            try
            {
                var client = new Client(apiKey: apiKey);


                var countrySchema = new Schema
                {
                    Type = Google.GenAI.Types.Type.OBJECT,
                    Title = "CountryInfo",
                    Properties = new Dictionary<string, Schema>
                        {
                            { "title", new Schema { Type = Google.GenAI.Types.Type.STRING, Title = "Title" } },
                            { "population", new Schema { Type = Google.GenAI.Types.Type.INTEGER, Title = "Population" } },
                            { "capital", new Schema { Type = Google.GenAI.Types.Type.STRING, Title = "Capital" } },
                            { "continent", new Schema { Type = Google.GenAI.Types.Type.STRING, Title = "Continent" } },
                            { "language", new Schema { Type = Google.GenAI.Types.Type.STRING, Title = "Language" } }
                        },
                    Required = new List<string> { "title", "population", "capital", "continent", "language" },
                    PropertyOrdering = new List<string> { "title", "population", "capital", "continent", "language" }
                };


                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: $"Give me information about {countryName}",
                    config: new GenerateContentConfig
                    {
                        ResponseMimeType = "application/json",
                        ResponseSchema = countrySchema
                    }
                );


                string jsonText = response.Candidates[0].Content.Parts[0].Text;


                return Content(jsonText, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest($"發生錯誤: {ex.Message}");
            }
        }
    }
            public class MessageRequest
            {
                public string UserContent { get; set; }
            }
    }






