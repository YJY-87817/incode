using System.Net;
using System.Text;
using incode.MVC.Dtos.Movies;
using incode.MVC.Services.Movies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace incode.MVC.Tests;

/// <summary>首頁近期上映電影（TMDB）：日期區間、轉換規則、快取與錯誤處理。TMDB 以假的 HttpMessageHandler 取代。</summary>
public class TmdbServiceTests
{
    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class BlockingHandler(Func<Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond();
    }

    private static TmdbService CreateWith(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["TmdbApiKey"] = "t" }).Build();
        return new TmdbService(http, config, new MemoryCache(new MemoryCacheOptions()), new FixedTime(Now), NullLogger<TmdbService>.Instance);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    // 台灣時間 2026-11-27 12:00
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 4, 0, 0, TimeSpan.Zero);

    private static (TmdbService Service, FakeHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond, string? token = "v4-token", DateTimeOffset? now = null)
    {
        var handler = new FakeHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["TmdbApiKey"] = token }).Build();
        var service = new TmdbService(http, config, new MemoryCache(new MemoryCacheOptions()), new FixedTime(now ?? Now), NullLogger<TmdbService>.Instance);
        return (service, handler);
    }

    // ───────── 日期區間 ─────────

    [Theory]
    [InlineData("2026-11-27", "2026-10-01", "2026-12-31")]
    [InlineData("2026-01-15", "2025-12-01", "2026-02-28")]
    [InlineData("2026-12-31", "2026-11-01", "2027-01-31")]
    [InlineData("2028-01-31", "2027-12-01", "2028-02-29")]
    public void Range_is_previous_month_start_to_next_month_end(string today, string from, string to)
    {
        var (f, t) = TmdbService.GetReleaseRange(DateOnly.Parse(today));
        Assert.Equal(DateOnly.Parse(from), f);
        Assert.Equal(DateOnly.Parse(to), t);
    }

    [Fact]
    public async Task Today_uses_taipei_time_not_utc()
    {
        // UTC 2026-11-30 17:00 = 台灣 2026-12-01 01:00 → 區間是 11/01～01/31
        var (service, handler) = Create(_ => Json("""{"results":[],"total_pages":1}"""), now: new DateTimeOffset(2026, 11, 30, 17, 0, 0, TimeSpan.Zero));
        await service.GetRecentReleasesAsync();
        var url = handler.Requests[0].RequestUri!.ToString();
        Assert.Contains("discover/movie", url);
        Assert.Contains("release_date.gte=2026-11-01", url);
        Assert.Contains("release_date.lte=2027-01-31", url);
    }

    // ───────── 列表 ─────────

    [Fact]
    public async Task Recent_sends_taiwan_query_and_bearer_token_not_in_url()
    {
        var (service, handler) = Create(_ => Json("""{"results":[],"total_pages":1}"""));
        var result = await service.GetRecentReleasesAsync();

        Assert.True(result.Ok);
        var request = Assert.Single(handler.Requests);
        var url = request.RequestUri!.ToString();
        Assert.Contains("region=TW", url);
        Assert.Contains("language=zh-TW", url);
        Assert.Contains("release_date.gte=2026-10-01", url);
        Assert.Contains("release_date.lte=2026-12-31", url);
        Assert.DoesNotContain("v4-token", url);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("v4-token", request.Headers.Authorization?.Parameter);
    }

    private static string TwDates(string date) => $$"""
        {"id":0,"results":[{"iso_3166_1":"TW","release_dates":[{"type":3,"release_date":"{{date}}T00:00:00.000Z"}]}]}
        """;

    [Fact]
    public async Task Recent_uses_taiwan_dates_sorts_newest_first_dedupes_and_skips_missing_posters()
    {
        var page1 = """
        {"total_pages":2,"results":[
          {"id":1,"title":"舊片","original_title":"Old","poster_path":"/a.jpg","release_date":"2026-10-05","vote_average":7.26},
          {"id":2,"title":"新片","original_title":"New","poster_path":"/b.jpg","release_date":"2026-12-20","vote_average":8.0},
          {"id":3,"title":"沒海報","original_title":"NoPoster","poster_path":null,"release_date":"2026-12-01","vote_average":5}
        ]}
        """;
        var page2 = """
        {"total_pages":2,"results":[
          {"id":2,"title":"新片(重複)","original_title":"New","poster_path":"/b.jpg","release_date":"2026-12-20","vote_average":8.0},
          {"id":4,"title":"國外早已上映","original_title":"Abroad","poster_path":"/c.jpg","release_date":"2026-06-10","vote_average":6.0},
          {"id":5,"title":"台灣日期不在區間","original_title":"Out","poster_path":"/d.jpg","release_date":"2026-11-01","vote_average":6.0}
        ]}
        """;
        var (service, handler) = Create(r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path.EndsWith("/discover/movie")) return Json(r.RequestUri.Query.Contains("page=2") ? page2 : page1);
            // 片 4：全球首映 6 月，台灣 12/05 上映 → 用台灣日期排序；片 5：台灣在 2027 才上映 → 排除；片 1：查不到 → 沿用首映日
            if (path.EndsWith("/movie/4/release_dates")) return Json(TwDates("2026-12-05"));
            if (path.EndsWith("/movie/5/release_dates")) return Json(TwDates("2027-03-01"));
            if (path.EndsWith("/movie/2/release_dates")) return Json(TwDates("2026-12-20"));
            return Json("{}", HttpStatusCode.NotFound);
        });

        var result = await service.GetRecentReleasesAsync();

        Assert.True(result.Ok);
        Assert.Equal(new[] { 2, 4, 1 }, result.Data!.Select(m => m.Id));
        Assert.Equal(new DateOnly(2026, 12, 5), result.Data![1].ReleaseDate);
        Assert.Equal(7.3, result.Data![2].VoteAverage);
        var discover = handler.Requests.Where(r => r.RequestUri!.AbsolutePath.EndsWith("/discover/movie")).ToList();
        Assert.Equal(2, discover.Count);
        Assert.All(discover, r => Assert.Contains("sort_by=popularity.desc", r.RequestUri!.ToString()));
    }

    [Fact]
    public async Task Recent_is_cached()
    {
        var (service, handler) = Create(r => r.RequestUri!.AbsolutePath.EndsWith("/discover/movie")
            ? Json("""{"results":[{"id":1,"title":"A","poster_path":"/a.jpg","release_date":"2026-11-01"}],"total_pages":1}""")
            : Json(TwDates("2026-11-02")));
        await service.GetRecentReleasesAsync();
        var second = await service.GetRecentReleasesAsync();

        Assert.True(second.Ok);
        Assert.Single(second.Data!);
        Assert.Equal(2, handler.Requests.Count); // discover + release_dates，第二次全部來自快取
    }

    [Fact]
    public async Task Concurrent_cache_misses_share_one_fetch()
    {
        var gate = new TaskCompletionSource();
        var calls = 0;
        var handler = new BlockingHandler(async () =>
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return Json("""{"status_code":34}""", HttpStatusCode.NotFound);
        });
        var service = CreateWith(handler);

        var tasks = Enumerable.Range(0, 5).Select(_ => service.GetMovieAsync(42)).ToList();
        await Task.Delay(100);
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(404, r.Code));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Failures_and_not_found_are_cached_briefly()
    {
        var status = HttpStatusCode.InternalServerError;
        var (service, handler) = Create(_ => Json("{}", status));

        await service.GetRecentReleasesAsync();
        var again = await service.GetRecentReleasesAsync();
        Assert.Equal(503, again.Code);
        Assert.Single(handler.Requests); // 503 也快取（FailureCacheDuration），TMDB 故障時不會每個請求都再呼叫

        status = HttpStatusCode.NotFound;
        await service.GetMovieAsync(7);
        await service.GetMovieAsync(7);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Missing_token_returns_503_without_calling_tmdb()
    {
        var (service, handler) = Create(_ => Json("{}"), token: "");

        var list = await service.GetRecentReleasesAsync();
        var detail = await service.GetMovieAsync(1);

        Assert.False(list.Ok);
        Assert.Equal(503, list.Code);
        Assert.Equal(503, detail.Code);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Tmdb_errors_return_503_with_generic_message(HttpStatusCode status)
    {
        var (service, _) = Create(_ => Json("""{"status_message":"secret internal detail"}""", status));

        var list = await service.GetRecentReleasesAsync();
        var detail = await service.GetMovieAsync(5);

        Assert.Equal(503, list.Code);
        Assert.Equal(TmdbService.UnavailableMessage, list.Message);
        Assert.Equal(503, detail.Code);
        Assert.DoesNotContain("secret", detail.Message);
    }

    [Fact]
    public async Task Network_failure_returns_503_without_exception_text()
    {
        var service = CreateWith(new FakeHandler(_ => throw new HttpRequestException("connection refused to 10.0.0.1")));

        var result = await service.GetRecentReleasesAsync();

        Assert.Equal(503, result.Code);
        Assert.DoesNotContain("10.0.0.1", result.Message);
    }

    // ───────── 詳細 ─────────

    private const string DetailJson = """
    {
      "id": 77, "title": "測試電影", "original_title": "Test Movie", "overview": "簡介",
      "poster_path": "/p.jpg", "backdrop_path": "/bg.jpg", "release_date": "2026-12-01",
      "runtime": 125, "vote_average": 7.84,
      "genres": [{"id":1,"name":"動作"},{"id":2,"name":"科幻"}],
      "credits": {
        "cast": [
          {"name":"演員B","character":"乙","order":1,"profile_path":"/b.jpg"},
          {"name":"演員A","character":"甲","order":0,"profile_path":null},
          {"name":"C","order":2},{"name":"D","order":3},{"name":"E","order":4},{"name":"F","order":5},
          {"name":"G","order":6},{"name":"H","order":7},{"name":"I","order":8},{"name":"J","order":9},{"name":"K","order":10}
        ],
        "crew": [{"name":"導演甲","job":"Director"},{"name":"編劇","job":"Writer"}]
      },
      "videos": {"results":[
        {"key":"enTeaser1","site":"YouTube","type":"Teaser","iso_639_1":"en","official":true},
        {"key":"enTrailer","site":"YouTube","type":"Trailer","iso_639_1":"en","official":true},
        {"key":"zhTrailer","site":"YouTube","type":"Trailer","iso_639_1":"zh","official":false},
        {"key":"vimeo0001","site":"Vimeo","type":"Trailer","iso_639_1":"zh","official":true},
        {"key":"bad key!!","site":"YouTube","type":"Trailer","iso_639_1":"zh","official":true}
      ]},
      "release_dates": {"results":[
        {"iso_3166_1":"US","release_dates":[{"type":3,"release_date":"2026-11-20T00:00:00.000Z"}]},
        {"iso_3166_1":"TW","release_dates":[
          {"type":4,"release_date":"2027-01-01T00:00:00.000Z"},
          {"type":3,"release_date":"2026-12-05T00:00:00.000Z"},
          {"type":2,"release_date":"2026-11-30T00:00:00.000Z"}]}
      ]}
    }
    """;

    [Fact]
    public async Task Detail_maps_fields_and_picks_best_trailer()
    {
        var (service, handler) = Create(_ => Json(DetailJson));

        var result = await service.GetMovieAsync(77);

        Assert.True(result.Ok);
        var m = result.Data!;
        Assert.Equal("測試電影", m.Title);
        Assert.Equal("Test Movie", m.OriginalTitle);
        Assert.Equal(125, m.Runtime);
        Assert.Equal(7.8, m.VoteAverage);
        Assert.Equal(new[] { "動作", "科幻" }, m.Genres);
        Assert.Equal(new[] { "導演甲" }, m.Directors);
        Assert.Equal("/bg.jpg", m.BackdropPath);
        // 台灣院線上映日（type 3）優先於 release_date
        Assert.Equal(new DateOnly(2026, 12, 5), m.ReleaseDate);
        // 依 order 排序、最多 10 位
        Assert.Equal(10, m.Cast.Count);
        Assert.Equal("演員A", m.Cast[0].Name);
        Assert.Equal("演員B", m.Cast[1].Name);
        Assert.Equal("/b.jpg", m.Cast[1].ProfilePath);
        // Trailer 優先、中文優先；Vimeo 與不合法的 key 略過
        Assert.Equal("zhTrailer", m.TrailerYoutubeKey);
        Assert.Contains("append_to_response=credits,videos,release_dates", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task Detail_without_taiwan_dates_falls_back_and_unsafe_values_are_dropped()
    {
        var json = """
        {"id":9,"title":"T","poster_path":"/../evil.jpg","backdrop_path":"https://evil.example/x.jpg","release_date":"2026-11-11","runtime":0,
         "videos":{"results":[{"key":"<script>","site":"YouTube","type":"Trailer"}]}}
        """;
        var (service, _) = Create(_ => Json(json));

        var m = (await service.GetMovieAsync(9)).Data!;

        Assert.Equal(new DateOnly(2026, 11, 11), m.ReleaseDate);
        Assert.Null(m.PosterPath);
        Assert.Null(m.BackdropPath);
        Assert.Null(m.Runtime);
        Assert.Null(m.TrailerYoutubeKey);
        Assert.Empty(m.Cast);
    }

    [Fact]
    public async Task Detail_not_found_returns_404_and_invalid_id_skips_request()
    {
        var (service, handler) = Create(_ => Json("""{"status_code":34}""", HttpStatusCode.NotFound));

        var missing = await service.GetMovieAsync(123456789);
        var invalid = await service.GetMovieAsync(0);

        Assert.Equal(404, missing.Code);
        Assert.Equal(TmdbService.NotFoundMessage, missing.Message);
        Assert.Equal(404, invalid.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Detail_is_cached()
    {
        var (service, handler) = Create(_ => Json(DetailJson));
        await service.GetMovieAsync(77);
        await service.GetMovieAsync(77);
        Assert.Single(handler.Requests);
    }
}

/// <summary>API 層：匿名可以呼叫，回應格式與狀態碼（TMDB 以假的服務取代）。</summary>
public class MovieApiTests
{
    private sealed class FakeTmdb : ITmdbService
    {
        public Task<ResultDTO<List<MovieSummaryDto>>> GetRecentReleasesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ResultDTO<List<MovieSummaryDto>>.Success(new() { new MovieSummaryDto { Id = 1, Title = "A", PosterPath = "/a.jpg", ReleaseDate = new DateOnly(2026, 11, 1) } }));

        public Task<ResultDTO<MovieDetailDto>> GetMovieAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == 1
                ? ResultDTO<MovieDetailDto>.Success(new MovieDetailDto { Id = 1, Title = "A" })
                : ResultDTO<MovieDetailDto>.Failure(TmdbService.NotFoundMessage, 404));
    }

    private sealed class Factory : ApiFactory
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITmdbService>();
                services.AddSingleton<ITmdbService, FakeTmdb>();
            });
        }
    }

    [Fact]
    public async Task Anonymous_can_get_recent_and_detail_and_unknown_is_404()
    {
        using var factory = new Factory();
        var client = factory.CreateAnonymousClient(ownIp: true);

        var recent = await client.GetAsync("/api/MovieApi/recent");
        Assert.Equal(HttpStatusCode.OK, recent.StatusCode);
        var body = await recent.Content.ReadAsStringAsync();
        Assert.Contains("\"posterPath\":\"/a.jpg\"", body);
        Assert.Contains("\"releaseDate\":\"2026-11-01\"", body);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/MovieApi/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/MovieApi/999")).StatusCode);
    }

    [Fact]
    public async Task Recent_is_not_rate_limited_per_ip_but_detail_is()
    {
        using var factory = new Factory();
        var client = factory.CreateAnonymousClient(ownIp: true);

        for (var i = 0; i < 70; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/MovieApi/recent")).StatusCode);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 61; i++)
            statuses.Add((await client.GetAsync("/api/MovieApi/1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
