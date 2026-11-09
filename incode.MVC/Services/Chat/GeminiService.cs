
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace incode.MVC.Services.Chat
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<GeminiService> _logger;

        // AI 服務失敗時顯示給使用者的訊息（細節只寫進 log，不放進聊天內容）
        public const string AiUnavailableMessage = "AI 客服暫時無法回覆，請稍後再試或改由真人客服協助。";

        public GeminiService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }


        // 純文字對話功能
        public async Task<string> GetGeminiReplyAsync(string fullPrompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(geminiApiKey))
            {
                _logger.LogError("GeminiApiKey 未設定");
                return AiUnavailableMessage;
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = fullPrompt } } } },
                generationConfig = new { temperature = 0.7, maxOutputTokens = 1000 }
            };

            return await SendToGemini(url, payload);
        }


        // 2. 純圖片分析 (舊功能)
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


        // Function Calling
        public async Task<string> AnalyzeImageWithToolAsync(IFormFile imageFile, string prompt)
        {
            if (imageFile == null || imageFile.Length == 0) return "錯誤：圖片為空";

            // 轉 Base64
            string base64Image;
            using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                base64Image = Convert.ToBase64String(ms.ToArray());
            }
            string mimeType = imageFile.ContentType;

            string geminiApiKey = _configuration["GeminiApiKey"];
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

            // 2. 組裝含有 Function Calling 的 Payload
            var payload = new
            {
                contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }, // 這裡會接收 Controller 傳來的 CALL_FUNCTION 規則
                        new { inline_data = new { mime_type = mimeType, data = base64Image } }
                    }
                }
            },
                        generationConfig = new { temperature = 0.0, maxOutputTokens = 1000 }
                    };

                    return await SendToGemini(url, payload);
                }


        // 核心：影像生成內容 (Gemini Vision 底層方法)
        public async Task<string> GetGeminiImageReplyAsync(string base64Image, string mimeType, string prompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"];
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={geminiApiKey}";

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

        // AI 生圖功能 (ModelsLab)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI 生圖失敗");
                return "圖片生成失敗，請稍後再試。";
            }
        }

        // ==========================================
        // 6. [Helper] 通用發送
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
                            var part = parts[0];

                            // 🔥 1. 檢查是否為 Function Call (工具呼叫)
                            if (part.TryGetProperty("functionCall", out var functionCall))
                            {
                                // 取得 AI 填寫的 JSON 參數字串
                                string argsJson = functionCall.GetProperty("args").GetRawText();
                                // 這裡回傳的會是乾淨的 JSON 字串，例如: {"ticketType":"電影優惠票","isPointsIndicated":false,"pointsValue":0}
                                return argsJson;
                            }

                            // 🔥 2. 如果不是 Function Call，才退回抓取純文字 (兼容你其他的對話功能)
                            if (part.TryGetProperty("text", out var textContent))
                            {
                                return textContent.GetString();
                            }
                        }
                    }
                    return "AI 思考中，但沒有回傳有效內容。";
                }
                else
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API 回應錯誤 {StatusCode}: {Details}", (int)response.StatusCode, errorDetails);
                    return AiUnavailableMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "呼叫 Gemini API 失敗");
                return AiUnavailableMessage;
            }
        }
    }
}