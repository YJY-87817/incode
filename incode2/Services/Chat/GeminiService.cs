using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting; // 新增：用於存取伺服器路徑
using System;
using System.IO; // 新增：用於檔案操作
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace incode2.Services.Chat
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment; // 新增：環境變數

        // 修改建構子：注入 IWebHostEnvironment
        public GeminiService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment; // 賦值
        }

        // ==========================================
        // 1. 純文字對話功能 (接收 Controller 組好的完整 Prompt)
        // ==========================================
        public async Task<string> GetGeminiReplyAsync(string fullPrompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(geminiApiKey)) return "[系統內部錯誤] Gemini API Key 未設定";

            // 使用 gemini-1.5-flash，速度快且穩定
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

            // 注意：這裡不再硬編碼 System Instruction，因為 Controller 已經讀取 .md 並傳進來了

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 800, //稍微增加 token 數，避免回答被截斷
                }
            };

            return await SendToGemini(url, payload);
        }


        // AI 生圖功能
        public async Task<string> GenerateImageAsync(string prompt)
        {
            // 1. 模擬 AI 思考時間
            await Task.Delay(1000);

            try
            {
                // 2. 優化提示詞
                // 加上 "cinematic" 等詞彙提升品質
                string enhancedPrompt = $"cinematic lighting, high quality, 8k, detailed, {prompt}";
                string safePrompt = Uri.EscapeDataString(enhancedPrompt);

                // 3. 組合 Pollinations API 網址 (修正版)
                // ★ 關鍵修改：
                // 1. 移除了 'nologo=true' (這通常需要 API Key)
                // 2. 移除了 'model=flux' (部分時段需要 Key，改用預設或 turbo)
                // 3. 加入 'seed' 隨機數，避免快取導致圖片不更新

                int randomSeed = new Random().Next(0, 999999);
                string imageUrl = $"https://image.pollinations.ai/prompt/{safePrompt}?width=1280&height=720&seed={randomSeed}&model=turbo";

                // 4. 回傳圖片標籤
                return $"<img src='{imageUrl}' alt='AI Generated Image' style='border-radius: 12px; max-width: 100%; box-shadow: 0 4px 12px rgba(0,0,0,0.1);' />";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pollinations Error]: {ex.Message}");
                // 萬一真的失敗，回傳一張靜態錯誤圖
                return "https://placehold.co/600x400?text=Service+Busy";
            }
        }

        // ==========================================
        // 3. 以圖搜圖功能
        // ==========================================
        public async Task<string> GetGeminiImageReplyAsync(string base64Image, string mimeType, string prompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(geminiApiKey)) return "[系統內部錯誤] API Key 未設定";

            // 使用支援 Vision 的模型
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Image
                                }
                            },
                            new { text = prompt }
                        }
                    }
                }
            };

            return await SendToGemini(url, payload);
        }

        // ==========================================
        // 4. [Helper] 存檔 Base64 圖片並回傳 URL
        // ==========================================
        private async Task<string> SaveBase64ImageAsync(string base64String)
        {
            // 1. 設定路徑: wwwroot/img/generated
            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "generated");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // 2. 產生檔名
            string fileName = $"gen_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 4)}.png";
            string filePath = Path.Combine(folderPath, fileName);

            // 3. 寫入檔案
            byte[] imageBytes = Convert.FromBase64String(base64String);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            // 4. 回傳相對網址 (給前端用)
            return $"/img/generated/{fileName}";
        }

        // ==========================================
        // 5. [Helper] 通用發送 (Text/Vision)
        // ==========================================
        private async Task<string> SendToGemini(string url, object payload)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IgnoreNullValues = true };
                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        // 嘗試安全地解析 Gemini 回傳結構
                        if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var candidate = candidates[0];

                            // 檢查是否因為安全性被阻擋 (SafetyRatings)
                            if (candidate.TryGetProperty("finishReason", out var finishReason) && finishReason.GetString() == "SAFETY")
                            {
                                return "抱歉，您的請求內容涉及敏感議題，AI 無法回答。";
                            }

                            if (candidate.TryGetProperty("content", out var content) &&
                                content.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0)
                            {
                                return parts[0].GetProperty("text").GetString();
                            }
                        }
                        return "AI 思考中，但沒有產生回應。";
                    }
                }

                string errorMsg = await response.Content.ReadAsStringAsync();
                return $"[API 錯誤] {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"[連線錯誤] {ex.Message}";
            }
        }
    }
}