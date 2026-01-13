using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web; // 需要 System.Web for UrlEncode (如果是 .NET Core 可能需用 System.Net.WebUtility)

namespace incode2.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // 透過 DI 注入 HttpClient 和 Configuration
        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetGeminiReplyAsync(string userContent)
        {
            // 1. 讀取 Keys (建議放在 secrets.json)
            string geminiApiKey = _configuration["GeminiApiKey"];
            string tmdbApiKey = _configuration["TmdbApiKey"];

            if (string.IsNullOrEmpty(geminiApiKey)) return "系統錯誤：Gemini API Key 未設定";

            // 2. TMDB 邏輯
            string currentDate = DateTime.Now.ToString("yyyy年MM月dd日");
            string tmdbInfo = null;

            // 簡易判斷
            bool isMovieQuery = userContent.Contains("電影") || userContent.Contains("movie") ||
                                userContent.Contains("上映") || userContent.Contains("演");

            if (!string.IsNullOrEmpty(tmdbApiKey) && isMovieQuery)
            {
                tmdbInfo = await SearchTmdbApi(userContent, tmdbApiKey);
            }

            // 3. 組合 Prompt
            string finalPrompt = BuildPrompt(userContent, tmdbInfo, currentDate);

            // 4. 呼叫 Gemini
            return await CallGeminiApi(finalPrompt, geminiApiKey);
        }

        private string BuildPrompt(string userContent, string tmdbInfo, string currentDate)
        {
            if (!string.IsNullOrEmpty(tmdbInfo))
            {
                // 情況 1: 有 TMDB 資料
                return $@"
                    【系統強制設定】：
                    1. 現在的時間是：{currentDate} (我們身處未來)。
                    2. 你的身分是 2026 年的電影評論家。
                    
                    【輸入資料】：
                    - 參考資料 (舊資料庫)：{tmdbInfo}
                    - 使用者問題：{userContent}
                    
                    【決策邏輯】：
                    若使用者問舊電影，參考資料回答。
                    若使用者問「最新」、「今年(2026)」電影，請忽略舊資料，直接「創作」或「預測」2026 年的上映清單 (如阿凡達3、蜘蛛人4等)，語氣要篤定。
                ";
            }
            else
            {
                // 情況 2: 無 TMDB 資料
                return $@"
                    【系統設定】：
                    現在時間是 {currentDate}。
                    使用者詢問電影相關問題，但目前沒有具體資料。
                    請發揮創意，扮演 2026 年的電影達人，直接推薦幾部這個時間點(2026)可能正在熱映的電影。
                    
                    【使用者問題】：
                    {userContent}";
            }
        }

        private async Task<string> SearchTmdbApi(string query, string apiKey)
        {
            try
            {
                string cleanQuery = query.Replace("電影", "").Replace("movie", "").Replace("上映", "").Trim();
                string encodedQuery = System.Net.WebUtility.UrlEncode(cleanQuery); // .NET Core 建議寫法
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
                            var movie = results[0];
                            string title = movie.GetProperty("title").GetString();
                            string overview = movie.GetProperty("overview").GetString();
                            string date = movie.TryGetProperty("release_date", out var d) ? d.GetString() : "未知";
                            double vote = movie.TryGetProperty("vote_average", out var v) ? v.GetDouble() : 0;

                            return $"片名：{title}\n上映日：{date}\n評分：{vote}/10\n劇情簡介：{overview}";
                        }
                    }
                }
                return null;
            }
            catch { return null; }
        }

        private async Task<string> CallGeminiApi(string prompt, string apiKey)
        {
            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}"; // 注意模型版本
                var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        return doc.RootElement.GetProperty("candidates")[0]
                                              .GetProperty("content")
                                              .GetProperty("parts")[0]
                                              .GetProperty("text")
                                              .GetString();
                    }
                }
                return $"Gemini API 錯誤: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"AI 回應解析失敗: {ex.Message}";
            }
        }
    }
}