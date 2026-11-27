namespace incode.MVC.Services.Movies
{
    public interface ITmdbService
    {
        /// <summary>台灣上映日在「上個月 1 日～下個月月底」的電影，依上映日由新到舊。</summary>
        Task<ResultDTO<List<MovieSummaryDto>>> GetRecentReleasesAsync(CancellationToken cancellationToken = default);

        /// <summary>單部電影的詳細資料（含導演、演員、預告片）。找不到回 404，TMDB 無法使用回 503。</summary>
        Task<ResultDTO<MovieDetailDto>> GetMovieAsync(int id, CancellationToken cancellationToken = default);
    }
}
