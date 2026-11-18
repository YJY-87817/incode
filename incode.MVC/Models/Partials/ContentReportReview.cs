namespace incode.MVC.Models
{
    /// <summary>
    /// 後台處理檢舉的紀錄（content_report_review，不是 Database-First 產生的資料表；設定在 incodedatabaseContext.OnModelCreatingPartial）。
    /// 一篇文章最後一次處理之後才出現的檢舉，才會再列入「待處理」。
    /// </summary>
    public class ContentReportReview
    {
        public int ContentReportReviewId { get; set; }

        public int ContentId { get; set; }

        /// <summary>處理的管理員</summary>
        public int AdminUserId { get; set; }

        /// <summary><see cref="ContentReportActions"/>：dismissed（駁回）、removed（下架）、restored（恢復上架）</summary>
        public string Action { get; set; } = "";

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual Content Content { get; set; } = null!;

        public virtual User AdminUser { get; set; } = null!;
    }
}
