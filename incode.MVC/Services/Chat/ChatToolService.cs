using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Chat
{
    /// <summary>AI 客服可以呼叫的後端工具（AI 回覆 <c>CALL_FUNCTION:工具名稱|關鍵字</c>，見 Prompts/Prompts.md）。</summary>
    public enum ChatTool
    {
        GetProductInfo,
        SearchContents,
        PointLogs,
    }

    public interface IChatToolService
    {
        /// <summary>上架中的商品（名稱 / 說明 / 類型 / 系列符合關鍵字），JSON 陣列；查無資料回傳說明文字。</summary>
        Task<string> SearchProductsAsync(string keyword);

        /// <summary>公開的主題文章（標題符合關鍵字，最新的優先），JSON 陣列；查無資料回傳說明文字。</summary>
        Task<string> SearchContentsAsync(string keyword);

        /// <summary>該會員目前的點數餘額與最近的點數紀錄（只查詢登入者本人）。</summary>
        Task<string> GetPointSummaryAsync(int userId);
    }

    /// <summary>
    /// AI 客服工具的資料查詢。原本是寫死的假資料（例如餘額一律 1500、userId 1），會員問點數會得到錯的答案。
    /// 回傳的 JSON 會放進給 Gemini 的提示詞，只包含可以讓會員看到的欄位。
    /// </summary>
    public class ChatToolService : IChatToolService
    {
        internal const int MaxProducts = 3;
        internal const int MaxContents = 5;
        internal const int MaxPointLogs = 5;
        private const int MaxKeywordLength = 50;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
        };

        private readonly incodedatabaseContext _context;
        private readonly IPointLogService _pointLogService;

        public ChatToolService(incodedatabaseContext context, IPointLogService pointLogService)
        {
            _context = context;
            _pointLogService = pointLogService;
        }

        /// <summary>
        /// 解析 AI 第一次回覆中的工具呼叫（<c>CALL_FUNCTION:名稱|關鍵字</c>）。不是工具呼叫或格式不符回傳 null，
        /// 呼叫端直接把回覆當成答案（原本以 <c>Split('|')[1]</c> 取關鍵字，AI 沒附「|」時 500）。
        /// </summary>
        public static (ChatTool Tool, string Keyword)? ParseToolCall(string? aiResponse)
        {
            const string marker = "CALL_FUNCTION:";
            if (string.IsNullOrEmpty(aiResponse)) return null;

            int start = aiResponse.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return null;

            // 只看標記所在的那一行
            string call = aiResponse[(start + marker.Length)..];
            int lineEnd = call.IndexOfAny(['\r', '\n']);
            if (lineEnd >= 0) call = call[..lineEnd];

            int bar = call.IndexOf('|');
            string name = (bar >= 0 ? call[..bar] : call).Trim();
            string keyword = bar >= 0 ? call[(bar + 1)..].Trim().Trim('{', '}', '「', '」', '"', '\'').Trim() : "";

            // 只接受名稱（Enum.TryParse 也接受數字字串）
            if (name.Length == 0 || !char.IsLetter(name[0])
                || !Enum.TryParse<ChatTool>(name, ignoreCase: false, out var tool) || !Enum.IsDefined(tool))
                return null;

            if (keyword.Length > MaxKeywordLength) keyword = keyword[..MaxKeywordLength];
            return (tool, keyword);
        }

        public async Task<string> SearchProductsAsync(string keyword)
        {
            foreach (var term in SearchTerms(keyword))
            {
                var products = await _context.Product
                    .AsNoTracking()
                    .Where(p => p.OnSale && (p.Name.Contains(term) || p.Description.Contains(term)
                        || p.Type.Contains(term) || p.Series.Contains(term)))
                    .OrderBy(p => p.Name.Contains(term) ? 0 : 1)
                    .ThenByDescending(p => p.ProductId)
                    .Take(MaxProducts)
                    .Select(p => new
                    {
                        productId = p.ProductId,
                        name = p.Name,
                        description = p.Description,
                        imageName = p.ImageName,
                        type = p.Type,
                        point = p.Point,
                        inStock = p.Stock > 0,
                    })
                    .ToListAsync();

                if (products.Count > 0) return JsonSerializer.Serialize(products, JsonOptions);
            }
            return "查無相關商品資料。";
        }

        public async Task<string> SearchContentsAsync(string keyword)
        {
            // 內文存成檔案路徑，只能以標題搜尋；只列公開（IsActive = 1）的主題文章，不含回覆
            foreach (var term in SearchTerms(keyword))
            {
                var contents = await _context.Content
                    .AsNoTracking()
                    .Where(c => c.IsActive == 1 && c.ParentContentId == null && c.Title.Contains(term))
                    .OrderByDescending(c => c.CreatedAt)
                    .ThenByDescending(c => c.ContentId)
                    .Take(MaxContents)
                    .Select(c => new
                    {
                        contentId = c.ContentId,
                        title = c.Title,
                        categoryName = c.Category != null ? c.Category.CategoryName : null,
                        username = c.User.Username,
                    })
                    .ToListAsync();

                if (contents.Count > 0) return JsonSerializer.Serialize(contents, JsonOptions);
            }
            return "查無相關文章。";
        }

        public async Task<string> GetPointSummaryAsync(int userId)
        {
            int balance = await _pointLogService.GetCurrentBalanceAsync(userId);

            var recentLogs = await _context.PointLog
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.PointLogId)
                .Take(MaxPointLogs)
                .Select(l => new
                {
                    displayName = l.DisplaySource,
                    changeAmount = l.ChangeAmount,
                    balanceAfter = l.BalanceAfter,
                    createdAt = l.CreatedAt,
                })
                .ToListAsync();

            return JsonSerializer.Serialize(new { currentBalance = balance, recentLogs }, JsonOptions);
        }

        // 先以完整關鍵字搜尋，找不到再逐一以空白分隔的單字搜尋
        private static IEnumerable<string> SearchTerms(string keyword)
        {
            keyword = (keyword ?? "").Trim();
            if (keyword.Length == 0) yield break;

            yield return keyword;

            var words = keyword.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1) yield break;
            foreach (var word in words.Distinct())
                yield return word;
        }
    }
}
