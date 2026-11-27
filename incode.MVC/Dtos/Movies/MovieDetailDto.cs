namespace incode.MVC.Dtos.Movies
{
    public class MovieCastDto
    {
        public string Name { get; set; } = "";
        public string? Character { get; set; }
        public string? ProfilePath { get; set; }
    }

    /// <summary>電影介紹頁用的詳細資料（一次向 TMDB 取得基本資料、演職員與影片）。</summary>
    public class MovieDetailDto : MovieSummaryDto
    {
        public string? BackdropPath { get; set; }
        public string? Overview { get; set; }
        /// <summary>片長（分鐘），TMDB 沒有資料時為 null</summary>
        public int? Runtime { get; set; }
        public List<string> Genres { get; set; } = new();
        public List<string> Directors { get; set; } = new();
        public List<MovieCastDto> Cast { get; set; } = new();
        /// <summary>預告片的 YouTube 影片代碼（已驗證格式），沒有預告片為 null</summary>
        public string? TrailerYoutubeKey { get; set; }
    }
}
