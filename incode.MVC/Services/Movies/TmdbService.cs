using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Services.Movies
{
    /// <summary>
    /// 向 TMDB（The Movie Database）取得電影資料。
    /// 設定 <c>TmdbApiKey</c> 放 v4「API Read Access Token」（以 Authorization: Bearer 傳送，不放網址）。
    /// 結果以記憶體快取，所有訪客共用，避免每個人進首頁都呼叫 TMDB。
    /// </summary>
    public partial class TmdbService : ITmdbService
    {
        public const string UnavailableMessage = "電影資訊暫時無法取得，請稍後再試";
        public const string NotFoundMessage = "找不到這部電影";

        public static readonly TimeSpan ListCacheDuration = TimeSpan.FromHours(6);
        public static readonly TimeSpan DetailCacheDuration = TimeSpan.FromHours(24);

        private const int MaxListPages = 2;
        private const int MaxCast = 10;

        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly TimeProvider _time;
        private readonly ILogger<TmdbService> _logger;

        public TmdbService(HttpClient http, IConfiguration configuration, IMemoryCache cache, TimeProvider time, ILogger<TmdbService> logger)
        {
            _http = http;
            _configuration = configuration;
            _cache = cache;
            _time = time;
            _logger = logger;
        }

        // 台灣時間的「今天」（伺服器可能在其他時區）
        private static readonly TimeZoneInfo TaipeiZone = FindTaipeiZone();

        private static TimeZoneInfo FindTaipeiZone()
        {
            foreach (var id in new[] { "Asia/Taipei", "Taipei Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
            }
            return TimeZoneInfo.CreateCustomTimeZone("UTC+8", TimeSpan.FromHours(8), "UTC+8", "UTC+8");
        }

        /// <summary>上個月 1 日～下個月月底</summary>
        public static (DateOnly From, DateOnly To) GetReleaseRange(DateOnly today)
        {
            var thisMonth = new DateOnly(today.Year, today.Month, 1);
            var from = thisMonth.AddMonths(-1);
            var nextMonth = thisMonth.AddMonths(1);
            var to = nextMonth.AddMonths(1).AddDays(-1);
            return (from, to);
        }

        private DateOnly TodayInTaipei()
            => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_time.GetUtcNow(), TaipeiZone).DateTime);

        // ───────── 列表 ─────────

        public async Task<ResultDTO<List<MovieSummaryDto>>> GetRecentReleasesAsync(CancellationToken cancellationToken = default)
        {
            var token = _configuration["TmdbApiKey"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("TmdbApiKey 未設定");
                return ResultDTO<List<MovieSummaryDto>>.Failure(UnavailableMessage, 503);
            }

            var (from, to) = GetReleaseRange(TodayInTaipei());
            var cacheKey = $"tmdb:recent:{from:yyyyMMdd}:{to:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<MovieSummaryDto>? cached) && cached != null)
                return ResultDTO<List<MovieSummaryDto>>.Success(cached);

            try
            {
                var movies = new Dictionary<int, MovieSummaryDto>();
                for (var page = 1; page <= MaxListPages; page++)
                {
                    var url = "discover/movie?region=TW&language=zh-TW&include_adult=false&with_release_type=2|3"
                        + $"&release_date.gte={from:yyyy-MM-dd}&release_date.lte={to:yyyy-MM-dd}"
                        + $"&sort_by=primary_release_date.desc&page={page}";

                    using var doc = await SendAsync(url, token, cancellationToken);
                    if (doc == null) return ResultDTO<List<MovieSummaryDto>>.Failure(UnavailableMessage, 503);

                    var root = doc.RootElement;
                    if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in results.EnumerateArray())
                        {
                            var movie = ParseSummary(item);
                            // 沒有海報的片放進輪播很難看，直接略過
                            if (movie == null || string.IsNullOrEmpty(movie.PosterPath)) continue;
                            movies.TryAdd(movie.Id, movie);
                        }
                    }

                    var totalPages = root.TryGetProperty("total_pages", out var tp) && tp.TryGetInt32(out var n) ? n : 1;
                    if (page >= totalPages) break;
                }

                var list = movies.Values
                    .OrderByDescending(m => m.ReleaseDate ?? DateOnly.MinValue)
                    .ThenByDescending(m => m.VoteAverage)
                    .ToList();

                _cache.Set(cacheKey, list, ListCacheDuration);
                return ResultDTO<List<MovieSummaryDto>>.Success(list);
            }
            catch (TmdbNotAvailableException)
            {
                return ResultDTO<List<MovieSummaryDto>>.Failure(UnavailableMessage, 503);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                _logger.LogError(ex, "向 TMDB 取得近期上映電影失敗");
                return ResultDTO<List<MovieSummaryDto>>.Failure(UnavailableMessage, 503);
            }
        }

        // ───────── 詳細 ─────────

        public async Task<ResultDTO<MovieDetailDto>> GetMovieAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return ResultDTO<MovieDetailDto>.Failure(NotFoundMessage, 404);

            var token = _configuration["TmdbApiKey"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("TmdbApiKey 未設定");
                return ResultDTO<MovieDetailDto>.Failure(UnavailableMessage, 503);
            }

            var cacheKey = $"tmdb:movie:{id}";
            if (_cache.TryGetValue(cacheKey, out MovieDetailDto? cached) && cached != null)
                return ResultDTO<MovieDetailDto>.Success(cached);

            try
            {
                var url = $"movie/{id}?language=zh-TW&append_to_response=credits,videos,release_dates&include_video_language=zh,en,null";
                using var doc = await SendAsync(url, token, cancellationToken, notFoundIsNull: true);
                if (doc == null) return ResultDTO<MovieDetailDto>.Failure(NotFoundMessage, 404);

                var detail = ParseDetail(doc.RootElement);
                if (detail == null) return ResultDTO<MovieDetailDto>.Failure(UnavailableMessage, 503);

                _cache.Set(cacheKey, detail, DetailCacheDuration);
                return ResultDTO<MovieDetailDto>.Success(detail);
            }
            catch (TmdbNotAvailableException)
            {
                return ResultDTO<MovieDetailDto>.Failure(UnavailableMessage, 503);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                _logger.LogError(ex, "向 TMDB 取得電影 {MovieId} 失敗", id);
                return ResultDTO<MovieDetailDto>.Failure(UnavailableMessage, 503);
            }
        }

        // ───────── HTTP ─────────

        private sealed class TmdbNotAvailableException : Exception { }

        /// <summary>
        /// 送出 GET 並解析 JSON。<paramref name="notFoundIsNull"/> 為 true 時 404 回傳 null，其餘非 2xx 一律當成無法使用。
        /// 列表呼叫（notFoundIsNull = false）失敗時丟出 TmdbNotAvailableException 由呼叫端轉成 503。
        /// </summary>
        private async Task<JsonDocument?> SendAsync(string relativeUrl, string token, CancellationToken cancellationToken, bool notFoundIsNull = false)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound && notFoundIsNull)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                // 401 = Token 錯誤、429 = 超過 TMDB 流量，細節只寫 log
                _logger.LogError("TMDB 回應 {StatusCode}（{Path}）", (int)response.StatusCode, relativeUrl.Split('?')[0]);
                throw new TmdbNotAvailableException();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }

        // ───────── 解析 ─────────

        private static MovieSummaryDto? ParseSummary(JsonElement item)
        {
            if (!item.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id) || id <= 0) return null;
            var title = GetString(item, "title");
            var original = GetString(item, "original_title");
            return new MovieSummaryDto
            {
                Id = id,
                Title = !string.IsNullOrWhiteSpace(title) ? title! : original ?? "",
                OriginalTitle = original,
                PosterPath = SafeImagePath(GetString(item, "poster_path")),
                ReleaseDate = ParseDate(GetString(item, "release_date")),
                VoteAverage = item.TryGetProperty("vote_average", out var v) && v.ValueKind == JsonValueKind.Number ? Math.Round(v.GetDouble(), 1) : 0,
            };
        }

        private static MovieDetailDto? ParseDetail(JsonElement root)
        {
            var summary = ParseSummary(root);
            if (summary == null) return null;

            var detail = new MovieDetailDto
            {
                Id = summary.Id,
                Title = summary.Title,
                OriginalTitle = summary.OriginalTitle,
                PosterPath = summary.PosterPath,
                ReleaseDate = TaiwanReleaseDate(root) ?? summary.ReleaseDate,
                VoteAverage = summary.VoteAverage,
                BackdropPath = SafeImagePath(GetString(root, "backdrop_path")),
                Overview = GetString(root, "overview"),
                Runtime = root.TryGetProperty("runtime", out var rt) && rt.ValueKind == JsonValueKind.Number && rt.TryGetInt32(out var minutes) && minutes > 0 ? minutes : null,
            };

            if (root.TryGetProperty("genres", out var genres) && genres.ValueKind == JsonValueKind.Array)
                detail.Genres = genres.EnumerateArray().Select(g => GetString(g, "name")).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();

            if (root.TryGetProperty("credits", out var credits) && credits.ValueKind == JsonValueKind.Object)
            {
                if (credits.TryGetProperty("crew", out var crew) && crew.ValueKind == JsonValueKind.Array)
                    detail.Directors = crew.EnumerateArray()
                        .Where(c => GetString(c, "job") == "Director")
                        .Select(c => GetString(c, "name"))
                        .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!)
                        .Distinct().ToList();

                if (credits.TryGetProperty("cast", out var cast) && cast.ValueKind == JsonValueKind.Array)
                    detail.Cast = cast.EnumerateArray()
                        .Select(c => (Order: c.TryGetProperty("order", out var o) && o.TryGetInt32(out var oi) ? oi : int.MaxValue, Item: c))
                        .OrderBy(x => x.Order)
                        .Select(x => new MovieCastDto
                        {
                            Name = GetString(x.Item, "name") ?? "",
                            Character = GetString(x.Item, "character"),
                            ProfilePath = SafeImagePath(GetString(x.Item, "profile_path")),
                        })
                        .Where(c => c.Name != "")
                        .Take(MaxCast).ToList();
            }

            if (root.TryGetProperty("videos", out var videos) && videos.ValueKind == JsonValueKind.Object
                && videos.TryGetProperty("results", out var vids) && vids.ValueKind == JsonValueKind.Array)
                detail.TrailerYoutubeKey = PickTrailer(vids.EnumerateArray());

            return detail;
        }

        /// <summary>release_dates 裡台灣（TW）的院線上映日（type 3 戲院，其次 2 限定上映）；沒有則回 null</summary>
        private static DateOnly? TaiwanReleaseDate(JsonElement root)
        {
            if (!root.TryGetProperty("release_dates", out var rd) || rd.ValueKind != JsonValueKind.Object
                || !rd.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var country in results.EnumerateArray())
            {
                if (GetString(country, "iso_3166_1") != "TW") continue;
                if (!country.TryGetProperty("release_dates", out var dates) || dates.ValueKind != JsonValueKind.Array) continue;

                var candidates = dates.EnumerateArray()
                    .Select(d => (Type: d.TryGetProperty("type", out var t) && t.TryGetInt32(out var ti) ? ti : 0, Date: ParseDate(GetString(d, "release_date"))))
                    .Where(x => x.Date != null && (x.Type == 3 || x.Type == 2))
                    .ToList();

                var theatrical = candidates.Where(x => x.Type == 3).Select(x => x.Date!.Value).ToList();
                if (theatrical.Count > 0) return theatrical.Min();
                var limited = candidates.Select(x => x.Date!.Value).ToList();
                return limited.Count > 0 ? limited.Min() : null;
            }
            return null;
        }

        /// <summary>預告片：YouTube、Trailer 優先於 Teaser，語言 zh → en → 其他，官方優先。key 格式不合法的略過。</summary>
        private static string? PickTrailer(IEnumerable<JsonElement> videos)
        {
            var best = videos
                .Select(v => new
                {
                    Key = GetString(v, "key"),
                    Site = GetString(v, "site"),
                    Type = GetString(v, "type"),
                    Lang = GetString(v, "iso_639_1"),
                    Official = v.TryGetProperty("official", out var o) && o.ValueKind == JsonValueKind.True,
                })
                .Where(v => v.Site == "YouTube" && v.Key != null && YoutubeKeyRegex().IsMatch(v.Key) && (v.Type == "Trailer" || v.Type == "Teaser"))
                .OrderBy(v => v.Type == "Trailer" ? 0 : 1)
                .ThenBy(v => v.Lang == "zh" ? 0 : v.Lang == "en" ? 1 : 2)
                .ThenBy(v => v.Official ? 0 : 1)
                .FirstOrDefault();
            return best?.Key;
        }

        [GeneratedRegex("^[A-Za-z0-9_-]{6,20}$")]
        private static partial Regex YoutubeKeyRegex();

        // TMDB 的圖片路徑一律是 /xxx.jpg；格式不符的不回傳（前端會直接串進網址）
        [GeneratedRegex(@"^/[A-Za-z0-9_\-]+\.(jpg|jpeg|png|webp)$")]
        private static partial Regex ImagePathRegex();

        private static string? SafeImagePath(string? path)
            => path != null && ImagePathRegex().IsMatch(path) ? path : null;

        private static string? GetString(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;

        private static DateOnly? ParseDate(string? text)
            => DateOnly.TryParse(text?.Length >= 10 ? text[..10] : text, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
