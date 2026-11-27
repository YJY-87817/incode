using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace incode.MVC.ApiControllers.Movies
{
    /// <summary>首頁「近期上映」：電影資料來自 TMDB（後端轉呼叫並快取，Token 不會外露）。</summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class MovieApiController : ControllerBase
    {
        private readonly ITmdbService _tmdb;

        public MovieApiController(ITmdbService tmdb)
        {
            _tmdb = tmdb;
        }

        // GET api/MovieApi/recent（不需要輸入、結果由快取提供，不做依 IP 的速率限制：
        // 部署在代理後面或共用網路時，所有人共用一個 IP，首頁的輪播會被擋掉）
        // 台灣上映日在「上個月 1 日～下個月月底」的電影，依上映日由新到舊
        // Response: ResultDTO<[{ id, title, originalTitle, posterPath, releaseDate, voteAverage }]>
        [HttpGet("recent")]
        public async Task<ActionResult<ResultDTO<List<MovieSummaryDto>>>> GetRecent(CancellationToken cancellationToken)
            => this.ToActionResult(await _tmdb.GetRecentReleasesAsync(cancellationToken));

        // GET api/MovieApi/{id}
        // 單部電影：基本資料、導演、前 10 位演員、預告片（YouTube 影片代碼）；找不到 404
        // 可以指定任意 ID 繞過快取打 TMDB，依 IP 限制
        [HttpGet("{id:int}")]
        [EnableRateLimiting(RateLimitPolicies.Movie)]
        public async Task<ActionResult<ResultDTO<MovieDetailDto>>> GetMovie(int id, CancellationToken cancellationToken)
            => this.ToActionResult(await _tmdb.GetMovieAsync(id, cancellationToken));
    }
}
