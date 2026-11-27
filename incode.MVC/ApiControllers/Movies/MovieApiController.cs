using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace incode.MVC.ApiControllers.Movies
{
    /// <summary>首頁「近期上映」：電影資料來自 TMDB（後端轉呼叫並快取，Token 不會外露）。</summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Movie)]
    public class MovieApiController : ControllerBase
    {
        private readonly ITmdbService _tmdb;

        public MovieApiController(ITmdbService tmdb)
        {
            _tmdb = tmdb;
        }

        // GET api/MovieApi/recent
        // 台灣上映日在「上個月 1 日～下個月月底」的電影，依上映日由新到舊
        // Response: ResultDTO<[{ id, title, originalTitle, posterPath, releaseDate, voteAverage }]>
        [HttpGet("recent")]
        public async Task<ActionResult<ResultDTO<List<MovieSummaryDto>>>> GetRecent(CancellationToken cancellationToken)
            => this.ToActionResult(await _tmdb.GetRecentReleasesAsync(cancellationToken));

        // GET api/MovieApi/{id}
        // 單部電影：基本資料、導演、前 10 位演員、預告片（YouTube 影片代碼）；找不到 404
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResultDTO<MovieDetailDto>>> GetMovie(int id, CancellationToken cancellationToken)
            => this.ToActionResult(await _tmdb.GetMovieAsync(id, cancellationToken));
    }
}
