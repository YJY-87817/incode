using Google.GenAI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq; // ★ 新增
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic; // ★ 新增

namespace incode2.Services.Chat
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // ★ [核心新增] 文字向量化 (Embedding)
        // ==========================================
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            // 使用專門的 Embedding 模型：text-embedding-004
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={geminiApiKey}";

            var payload = new
            {
                model = "models/text-embedding-004",
                content = new { parts = new[] { new { text = text } } }
            };

            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);
                    // 解析回傳的向量陣列 (values)
                    var values = doc.RootElement.GetProperty("embedding").GetProperty("values");

                    return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
                }

                throw new Exception($"Embedding API 失敗: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                // 若失敗，回傳空陣列或拋出異常供 Controller 處理
                return Array.Empty<float>();
            }
        }

        // ==========================================
        // 1. 純文字對話功能 (更新模型名稱)
        // ==========================================
        public async Task<string> GetGeminiReplyAsync(string fullPrompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(geminiApiKey)) return "[系統內部錯誤] Gemini API Key 未設定";

            // 改用 1.5-flash，這是目前效能與速度最平衡的版本
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={geminiApiKey}";

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = fullPrompt } } } },
                generationConfig = new { temperature = 0.7, maxOutputTokens = 1000 }
            };

            return await SendToGemini(url, payload);
        }

        // ==========================================
        // 2. 圖片分析 (Controller 呼叫的入口)
        // ==========================================
        public async Task<string> AnalyzeImageAsync(IFormFile imageFile, string prompt)
        {
            if (imageFile == null || imageFile.Length == 0) return "錯誤：接收到的圖片為空";

            string base64Image;
            using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                base64Image = Convert.ToBase64String(ms.ToArray());
            }

            return await GetGeminiImageReplyAsync(base64Image, imageFile.ContentType, prompt);
        }
        

        // ==========================================
        // 3. 核心：影像生成內容 (Gemini Vision)
        // ==========================================
        public async Task<string> GetGeminiImageReplyAsync(string base64Image, string mimeType, string prompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={geminiApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new { inline_data = new { mime_type = mimeType, data = base64Image } }
                        }
                    }
                }
            };

            return await SendToGemini(url, payload);
        }

        // ==========================================
        // 4. AI 生圖功能 (ModelsLab)
        // ==========================================
        public async Task<string> GenerateImageAsync(string prompt)
        {
            string endpointUrl = "https://modelslab.com/api/v7/images/text-to-image";
            string apiKey = _configuration["imgapikey"];

            try
            {
                var requestData = new
                {
                    prompt = $"cinematic lighting, high quality, {prompt}",
                    model_id = "nano-banana-pro",
                    aspect_ratio = "1:1",
                    key = apiKey
                };

                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(endpointUrl, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                string status = root.TryGetProperty("status", out var s) ? s.GetString() : "";

                if (status == "success" && root.TryGetProperty("output", out var output) && output.GetArrayLength() > 0)
                {
                    string imageUrl = output[0].GetString();
                    return $"<img src='{imageUrl}' referrerpolicy='no-referrer' alt='AI Generated' style='border-radius: 12px; max-width: 100%; box-shadow: 0 4px 12px rgba(0,0,0,0.15);' />";
                }

                return status == "processing" ? "圖片生成中，請稍候 20 秒。" : "生圖服務暫時忙碌。";
            }
            catch (Exception ex) { return $"生成異常：{ex.Message}"; }
        }

        // ==========================================
        // 5. [Helper] 通用發送
        // ==========================================
        private async Task<string> SendToGemini(string url, object payload)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);

                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            return parts[0].GetProperty("text").GetString();
                        }
                    }
                    return "AI 思考中，但沒有回傳文字內容。";
                }
                else
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return $"[API 錯誤] 代碼: {response.StatusCode}\n詳細訊息: {errorDetails}";
                }
            }
            catch (Exception ex) { return $"[連線錯誤] {ex.Message}"; }
        }
    }
}