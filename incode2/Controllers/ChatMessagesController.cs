using Azure.Core;
using incode2.Data;
using incode2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http; //發送 API 請求
using System.Text;     //編碼
using System.Text.Json; //處理 JSON
using System.Threading.Tasks;

namespace incode2.Controllers
{
    // 強制此 Controller 使用 HTTPS (SSL)
    [RequireHttps]
    public class ChatMessagesController : Controller
    {
        private readonly incodedatabaseContext _context;
        private readonly IConfiguration _configuration;

        // 建立靜態 HttpClient 避免 Socket 耗盡問題 (效能優化)
        private static readonly HttpClient _httpClient = new HttpClient();

        public ChatMessagesController(incodedatabaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ==========================================
        // Part 1: 後台管理畫面 (MVC View)
        // ==========================================

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

        // ==========================================
        // Part 2: 前端 Vue 串接用的 API (接收訊息 -> 串接 -> 存檔)
        // ==========================================

        // [API] 接收 Vue 前端傳來的訊息
        [HttpPost]
        [Route("api/ChatMessages/SendMessage")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            // 1. 基本驗證
            if (request == null || string.IsNullOrEmpty(request.UserContent))
            {
                return BadRequest(new { error = "內容不能為空" });
            }

            // 讀取 API Keys
            string geminiApiKey = _configuration["GeminiApiKey"];
            string tmdbApiKey = _configuration["TmdbApiKey"];

            if (string.IsNullOrEmpty(geminiApiKey)) return BadRequest(new { error = "API Key 未設定" });

            try
            {
                // 2. 處理 Session (若 ID 為 0，建立新對話)
                long currentSessionId = request.SessionId;
                if (currentSessionId <= 0)
                {
                    var newSession = new ChatSession
                    {
                        // 防止 Title 過長導致報錯
                        Title = request.UserContent.Length > 20 ? request.UserContent.Substring(0, 20) : request.UserContent,
                        CreatedAt = DateTime.Now,
                        UserId = 1
                    };
                    _context.ChatSession.Add(newSession);
                    await _context.SaveChangesAsync();
                    currentSessionId = newSession.ChatSessionId;
                }

                // 3. 【存檔-使用者】
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
                //  核心邏輯修改：TMDB 優先策略
                // ==========================================

                // 1. 取得現在時間 (模擬的 2026)
                string currentDate = DateTime.Now.ToString("yyyy年MM月dd日");

                string tmdbInfo = null;
                string finalPrompt = request.UserContent;

                // 簡易判斷是否為電影相關問題
                bool isMovieQuery = request.UserContent.Contains("電影") ||
                                    request.UserContent.Contains("movie") ||
                                    request.UserContent.Contains("上映") ||
                                    request.UserContent.Contains("演");

                // 步驟 A: 如果是電影問題，先呼叫 TMDB
                // 注意：如果使用者只輸入「最近有什麼電影」，TMDB 搜尋可能會失敗(因為它預期片名)，
                // 這時 tmdbInfo 會是 null，剛好會掉入下面的「情況 2」，這符合我們的需求。
                if (!string.IsNullOrEmpty(tmdbApiKey) && isMovieQuery)
                {
                    tmdbInfo = await SearchTmdbApi(request.UserContent, tmdbApiKey);
                }

                // 步驟 B: 根據 TMDB 結果決定 Prompt 策略
                if (!string.IsNullOrEmpty(tmdbInfo))
                {
                    // --- 情況 1: TMDB 找到了資料 (但可能是 2024 的舊資料) ---
                    finalPrompt = $@"
                    【系統強制設定】：
                    1. 現在的時間是：{currentDate} (我們身處未來)。
                    2. 你的身分是 2026 年的電影評論家。
                    
                    【輸入資料】：
                    - 參考資料 (舊資料庫)：{tmdbInfo}
                    - 使用者問題：{request.UserContent}
                    
                    【決策邏輯 (請嚴格遵守)】：
                    判斷使用者的意圖：
                    A. 若使用者問「某部特定舊電影」(如：沙丘2評價如何？)：
                       -> 請直接使用【參考資料】回答，並加上你的評論。
                    
                    B. 若使用者問「最新」、「最近」、「今年(2026)」的電影：
                       -> **檢查**：【參考資料】裡的電影如果是 2024/2025 年以前的，請視為「過時資料」並忽略它。
                       -> **行動**：請直接「創作」或「預測」 2026 年上映的電影清單。
                       -> **創作方向**：可以提《阿凡達3》、《復仇者聯盟：康之王朝》、《蜘蛛人4》或是你虛構的科幻大片。
                       -> **語氣**：請用篤定的語氣，例如：「最近最紅的當然是剛上映的《阿凡達3》，特效完全超越前作...」。
                    ";
                }
                else
                {
                    // --- 情況 2: TMDB 沒資料 (或是搜尋關鍵字太廣泛導致沒結果) ---
                    finalPrompt = $@"
                    【系統設定】：
                    現在時間是 {currentDate}。
                    使用者詢問電影相關問題，但目前沒有具體資料。
                    
                    【任務】：
                    請發揮你的創意，扮演一位 2026 年的電影達人。
                    既然查不到資料，請直接根據「現在是 2026 年」的設定，推薦幾部這個時間點可能正在熱映的電影（可以是續集電影或虛構的強檔片）。
                    回答要像真的一樣，不要透露你是因為查不到資料才編的。
                    
                    【使用者問題】：
                    {request.UserContent}";
                }

                // 步驟 C: 呼叫 Gemini (無論哪種情況，最後都由 AI 統一生成回應)
                string aiResponseText = await CallGeminiApi(finalPrompt, geminiApiKey);

                // ==========================================

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

                // 5. 回傳 JSON
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

        // --- 輔助方法：呼叫 Gemini API (使用原生 HttpClient) ---
        private async Task<string> CallGeminiApi(string prompt, string apiKey)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    // 解析路徑: candidates[0].content.parts[0].text
                    try
                    {
                        return doc.RootElement.GetProperty("candidates")[0]
                                              .GetProperty("content")
                                              .GetProperty("parts")[0]
                                              .GetProperty("text")
                                              .GetString();
                    }
                    catch
                    {
                        return "AI 回應格式解析失敗";
                    }
                }
            }
            return $"Gemini API 錯誤: {response.StatusCode}";
        }

        // --- 輔助方法：呼叫 TMDB API ---
        private async Task<string> SearchTmdbApi(string query, string apiKey)
        {
            try
            {
                // 移除常見的雜訊字，提高搜尋準確度
                string cleanQuery = query.Replace("電影", "").Replace("movie", "").Replace("上映", "").Trim();

                string encodedQuery = System.Web.HttpUtility.UrlEncode(cleanQuery);
                string url = $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&language=zh-TW&query={encodedQuery}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var results = doc.RootElement.GetProperty("results");
                        if (results.GetArrayLength() > 0)
                        {
                            // 抓取第一筆 (最相關)
                            var movie = results[0];
                            string title = movie.GetProperty("title").GetString();
                            string overview = movie.GetProperty("overview").GetString();
                            string date = movie.TryGetProperty("release_date", out var d) ? d.GetString() : "未知";
                            double vote = movie.TryGetProperty("vote_average", out var v) ? v.GetDouble() : 0;

                            // 回傳格式化的資料字串
                            return $"片名：{title}\n上映日：{date}\n評分：{vote}/10\n劇情簡介：{overview}";
                        }
                    }
                }
                return null; // 找不到回傳 null
            }
            catch
            {
                return null; // 發生錯誤當作沒找到，讓 Gemini 接手
            }
        }
        // 定義接收前端資料的類別 (放置在 Namespace 內，Class 外)
        public class MessageRequest
        {
            public int SessionId { get; set; }  // 前端需傳入當前 SessionId (新對話傳 0)
            public string UserContent { get; set; } // 使用者的訊息
        }
    }
}