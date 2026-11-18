namespace incode.MVC.ViewModels.Contents
{
    /// <summary>後台檢舉清單（Views/ContentReport/Index）。</summary>
    public class ContentReportIndexViewModel
    {
        public List<ReportedContentItem> Pending { get; set; } = new();

        /// <summary>目前被下架（官方移除）的內容，可以恢復上架</summary>
        public List<ReportedContentItem> Removed { get; set; } = new();

        public List<ContentReportReviewItem> RecentReviews { get; set; } = new();
    }

    public class ReportedContentItem
    {
        public int ContentId { get; set; }
        public int Type { get; set; }
        /// <summary>主題標題；回覆類顯示所屬主題的標題</summary>
        public string? TopicTitle { get; set; }
        public int? TopicId { get; set; }
        public string? AuthorName { get; set; }
        public int Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        /// <summary>上次處理之後的檢舉數</summary>
        public int PendingReportCount { get; set; }
        public DateTime? LatestReportAt { get; set; }
    }

    public class ContentReportReviewItem
    {
        public int ContentId { get; set; }
        public string? TopicTitle { get; set; }
        public string Action { get; set; } = "";
        public string? AdminName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
