namespace incode2.Extensions
{
    /// <summary>
    /// 部署相關設定 (appsettings.json 的 "App" 區段)。
    /// 正式環境以環境變數覆寫，例如 App__FrontendUrl=https://www.example.com、App__CorsOrigins__0=https://www.example.com
    /// </summary>
    public static class ConfigurationExtensions
    {
        private const string DefaultFrontendUrl = "https://localhost:5173";

        /// <summary>前端網站網址 (不含結尾斜線)，用於登入導回、信件連結、金流付款完成導回、AI 回覆中的連結</summary>
        public static string GetFrontendUrl(this IConfiguration configuration)
            => (configuration["App:FrontendUrl"] is { Length: > 0 } url ? url : DefaultFrontendUrl).TrimEnd('/');

        /// <summary>CORS 允許來源：前端網址 + App:CorsOrigins 額外列出的來源 (例如 ngrok、其他開發 port)</summary>
        public static string[] GetCorsOrigins(this IConfiguration configuration)
            => new[] { configuration.GetFrontendUrl() }
                .Concat(configuration.GetSection("App:CorsOrigins").Get<string[]>() ?? Array.Empty<string>())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
