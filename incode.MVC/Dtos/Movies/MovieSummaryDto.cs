namespace incode.MVC.Dtos.Movies
{
    /// <summary>首頁輪播用的電影摘要。圖片只回 TMDB 的相對路徑（例如 /abc.jpg），由前端組成完整網址。</summary>
    public class MovieSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? OriginalTitle { get; set; }
        public string? PosterPath { get; set; }
        public DateOnly? ReleaseDate { get; set; }
        public double VoteAverage { get; set; }
    }
}
