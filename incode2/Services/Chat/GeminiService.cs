using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace incode2.Services.Chat
{
    // 定義 GeminiService 類別，這是一個封裝好的服務，負責與 Google Gemini API 進行溝通
    public class GeminiService
    {
        private readonly HttpClient _httpClient; // 宣告唯讀的 HttpClient 物件，用來發送 HTTP 網路請求 (例如 GET, POST)
        private readonly IConfiguration _configuration; // 宣告唯讀的 IConfiguration 物件，用來讀取專案設定檔 (appsettings.json)

        // 建構子 (Constructor)：透過依賴注入 (DI) 取得 HttpClient 與 Configuration 的實例
        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient; // 將注入的 HttpClient 指派給私有欄位，供類別內部使用
            _configuration = configuration; // 將注入的 Configuration 指派給私有欄位
        }

        // ==========================================
        // 1. 純文字對話功能
        // 參數 userContent: 使用者輸入的文字訊息
        // 回傳 Task<string>: 非同步回傳 AI 的回應字串
        // ==========================================
        public async Task<string> GetGeminiReplyAsync(string userContent)
        {
            string geminiApiKey = _configuration["GeminiApiKey"]; // 從 appsettings.json 中讀取名為 "GeminiApiKey" 的字串

            // 檢查 API Key 是否存在，若為空則直接回傳錯誤，避免發送無效請求
            if (string.IsNullOrEmpty(geminiApiKey)) return "[系統內部錯誤] Gemini API Key 未設定";

            // ★ 修正：將模型名稱指定為 gemini-1.5-flash，這是目前 Google 提供的免費且快速的穩定版本
            // 組合 API 請求的 URL，並將 API Key 以 Query String 的方式附加在後方
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key={geminiApiKey}";

            string currentTime = DateTime.Now.ToString("yyyy年MM月dd日"); // 取得伺服器當下的日期，並格式化為中文格式

            // 設定系統指令 (System Instruction)，這是一段 Prompt，用來定義 AI 的角色、語氣和限制
            // 這裡使用了 C# 的字串插值 ($) 和多行字串 (@) 功能
            string systemInstruction = $@"你是一個精簡扼要的電影 AI 助手。現在時間是 {currentTime}。
            嚴格遵守以下規則：
            1. 只能回答電影與影評相關問題。
            2. 回答請保持簡潔，若需推薦電影，請限制「最多 3 點」，若需推薦文章，請限制「最多 3 篇」。
            3. 每部電影的介紹請用「一句話」完成，不要廢話。
            4. 回答中請勿提及「現在時間」或「系統基準」等設定字眼。";

            // 組合最終要送給 AI 的提示詞，將「系統指令」與「使用者問題」合併
            // 特別要求使用繁體中文，確保回應符合台灣使用者習慣
            string finalPrompt = $"{systemInstruction}\n\n使用者問題：{userContent} (請用繁體中文(台灣)回答)";

            // 建立匿名物件 (Anonymous Object) 作為請求的 Payload (負載資料)
            // 這個結構必須符合 Google Gemini API 的 JSON 格式要求
            var payload = new
            {
                contents = new[] // contents 是一個陣列，包含對話的歷史或當前內容
                {
                    new
                    {
                        parts = new[] { new { text = finalPrompt } } // parts 包含實際的文字內容 (text)
                    }
                },

                generationConfig = new // 設定生成的配置參數，控制 AI 的回答風格
                {
                    temperature = 0.5, // 溫度參數 (0.0 - 1.0)。設為 0.5 代表平衡，既不會太死板也不會太隨機胡扯
                    maxOutputTokens = 70, // 限制 AI 回答的最大長度 (Token 數)，避免回答過長被截斷或浪費資源
                    topP = 0.9, // Nucleus sampling 參數，控制選詞的多樣性
                    topK = 20 // Top-K sampling 參數，限制選詞的大小
                }
            };

            // 呼叫共用的發送方法 SendToGemini，將網址和資料傳送出去，並等待結果
            return await SendToGemini(url, payload);
        }

        // ==========================================
        // 2. 以圖搜圖功能
        // 參數 base64Image: 圖片的 Base64 編碼字串
        // 參數 mimeType: 圖片的格式 (例如 image/jpeg, image/png)
        // 參數 prompt: 使用者針對這張圖片的文字提問
        // ==========================================
        public async Task<string> GetGeminiImageReplyAsync(string base64Image, string mimeType, string prompt)
        {
            string geminiApiKey = _configuration["GeminiApiKey"]; // 讀取 API Key
            if (string.IsNullOrEmpty(geminiApiKey)) return "[系統內部錯誤] Gemini API Key 未設定"; // 防呆檢查

            // ★ 修正：同樣使用 gemini-1.5-flash 模型，此模型支援視覺辨識 (Vision)
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key={geminiApiKey}";

            string currentTime = DateTime.Now.ToString("yyyy年MM月dd日"); // 取得當前時間

            // 組合針對圖片的系統指令，限制 AI 的回答範圍
            string finalPrompt = $"你是一個電影 AI。現在時間是 {currentTime}。請分析這張圖片內容是否與電影相關。{prompt} (注意：回答時請勿提及「現在時間」或「系統基準」，直接針對圖片內容回答)";

            // 建立圖片請求的 Payload，結構與純文字稍有不同
            var payload = new
            {
                contents = new[] // 對話內容陣列
                {
                    new
                    {
                        parts = new object[] // parts 陣列，這裡混合了「圖片資料」與「文字提示」
                        {
                            new
                            {
                                inline_data = new // 設定內嵌的二進位資料 (圖片)
                                {
                                    mime_type = mimeType, // 告訴 API 這是什麼格式的圖片 (如 image/png)
                                    data = base64Image // 填入圖片的 Base64 數據
                                }
                            },
                            new { text = finalPrompt } // 圖片後面的文字提示
                        }
                    }
                },
                generationConfig = new // 生成配置
                {
                    temperature = 0.7, // 圖片描述通常需要一點創意，所以溫度稍微調高到 0.7
                    maxOutputTokens = 2000, // 圖片分析的文字量通常較大，給予 2000 Token 的空間
                    topP = 0.9, // 取樣參數
                    topK = 40 // 取樣參數
                }
            };

            // 呼叫共用的發送方法
            return await SendToGemini(url, payload);
        }

        // ==========================================
        // 3. 共用的 HTTP 發送邏輯 (私有方法，不對外公開)
        // 參數 url: API 的完整網址
        // 參數 payload: 要傳送的資料物件 (C# 物件)
        // ==========================================
        private async Task<string> SendToGemini(string url, object payload)
        {
            try // 使用 try-catch 區塊來捕捉連線過程中可能發生的任何錯誤
            {
                // 設定 JSON 序列化選項：
                // PropertyNamingPolicy = CamelCase (將 C# 的 PascalCase 屬性名轉為 JSON 的 camelCase，例如 MaxOutputTokens -> maxOutputTokens)
                // IgnoreNullValues = true (忽略值為 null 的屬性，減少傳輸量)
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IgnoreNullValues = true };

                // 將 C# payload 物件序列化成 JSON 字串，並設定編碼為 UTF-8，Content-Type 為 application/json
                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json");

                // 使用 HttpClient 發送非同步 POST 請求到 Google API，並等待伺服器回應
                var response = await _httpClient.PostAsync(url, jsonContent);

                // A. 判斷 HTTP 狀態碼是否為成功 (200-299 之間)
                if (response.IsSuccessStatusCode)
                {
                    // 讀取回應內容為字串 (JSON 格式)
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // 使用 JsonDocument 解析 JSON 字串 (這比反序列化成物件更高效，適合唯讀)
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        // 取得根節點下的 "candidates" 陣列 (這是 Gemini 回傳候選答案的地方)
                        var candidates = doc.RootElement.GetProperty("candidates");

                        // 檢查是否有候選答案 (Array Length > 0)
                        if (candidates.GetArrayLength() > 0)
                        {
                            // 取得第一個候選答案的 "content" 物件
                            var content = candidates[0].GetProperty("content");
                            // 取得 "content" 下的 "parts" 陣列
                            var parts = content.GetProperty("parts");

                            // 檢查 parts 是否有內容
                            if (parts.GetArrayLength() > 0)
                            {
                                // 取出第一個 part 的 "text" 屬性，並轉為字串回傳 (這就是 AI 的最終回答)
                                return parts[0].GetProperty("text").GetString();
                            }
                        }
                    }
                    // 如果狀態碼成功，但解析不出內容 (極罕見情況)，回傳自定義錯誤訊息
                    return "[系統異常] AI 回傳了空的內容。";
                }

                // B. 如果 API 回傳錯誤狀態碼 (如 400 參數錯, 429 配額滿, 500 Google 內部錯誤)
                // 讀取錯誤的詳細內容 (通常包含 Google 給的錯誤原因 JSON)
                string errorMsg = await response.Content.ReadAsStringAsync();

                // 回傳帶有特定前綴 "[AI API 錯誤]" 的訊息，讓前端 Vue 可以識別並顯示紅字警告
                return $"[AI API 錯誤] Google 回傳狀態碼 {response.StatusCode}: {errorMsg}";
            }
            catch (HttpRequestException ex) // 捕捉 HTTP 請求層級的異常 (例如後端伺服器沒網路、DNS 解析失敗)
            {
                // C. 網路連線錯誤
                // 回傳帶有特定前綴 "[伺服器連線問題]" 的訊息
                return $"[伺服器連線問題] 後端無法連接到 Google Gemini: {ex.Message}";
            }
            catch (Exception ex) // 捕捉其他所有未預期的程式邏輯錯誤
            {
                // D. 其他內部錯誤
                return $"[系統內部錯誤] {ex.Message}";
            }
        }
    }
}