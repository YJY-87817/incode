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
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

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

        // ==========================================
        // 2. [新增] AI 生圖功能 (使用 Imagen 3 模型)
        // ==========================================
        public async Task<string> GenerateImageAsync(string prompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(geminiApiKey)) return "API Key Error";

            // 使用 Google Imagen 3 模型 (注意：需確保你的 API Key 專案有啟用此模型權限)
            // 如果 imagen-3.0-generate-001 無法使用，可嘗試 image-generation-002
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/imagen-3.0-generate-001:predict?key={geminiApiKey}";

            // Imagen 的 Payload 結構與 Gemini 不同
            var payload = new
            {
                instances = new[]
                {
                    new { prompt = $"High quality, cinematic movie scene style. {prompt}" } // 強制加上電影風格 Prompt
                },
                parameters = new
                {
                    sampleCount = 1, // 產生 1 張
                    aspectRatio = "16:9" // 電影比例
                }
            };

            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    // 如果生圖失敗，回傳一個預設錯誤圖片或文字
                    return "https://placehold.co/600x400?text=Image+Generation+Failed";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // 解析 Base64 圖片數據
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    // Imagen 回傳結構: predictions[0].bytesBase64Encoded
                    var predictions = doc.RootElement.GetProperty("predictions");
                    if (predictions.GetArrayLength() > 0)
                    {
                        string base64Image = predictions[0].GetProperty("bytesBase64Encoded").GetString();

                        // === 將 Base64 存為實體檔案 ===
                        return await SaveBase64ImageAsync(base64Image);
                    }
                }
                return "https://placehold.co/600x400?text=No+Image+Data";
            }
            catch (Exception ex)
            {
                return $"https://placehold.co/600x400?text=Error:+{ex.Message}";
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
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

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