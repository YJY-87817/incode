using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Point
{
    public class EntrustMissionDTO
    {
        public int? EntrustMissionId { get; set; }  // 查詢/更新時用，新增時可 null

        public int UserId { get; set; }             // 發起委託的使用者（後端自動設定，前端不需傳）

        [Required]
        public int ContentId { get; set; }          // 關聯內容

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Detail { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "賞金必須大於 0")]
        public int RewardPoints { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "其他";

        public DateTime? Deadline { get; set; }

        [Required]
        public string Status { get; set; } = "open";

        public bool IsUrgent { get; set; } = false;        // 改成非 nullable

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }           // 移除預設值，由後端設定

        public DateTime? UpdatedAt { get; set; }

        public int? BestContentId { get; set; }

        public int? BestUserId { get; set; }

        public DateTime? SelectedAt { get; set; }

        // === 前端顯示輔助欄位（後端 Select 時填充） ===
        public bool HasBestAnswer => BestContentId.HasValue;  // 方便前端判斷是否已結案

        public bool IsOwner { get; set; }                     // 是否為當前使用者發起的（後端比對 userId）

        public bool IsBestAnswerWinner { get; set; }

        // 發布者資訊（常用於列表顯示）
        public string? Username { get; set; }

        public string? ImageUrl { get; set; }

        // 最佳解答簡要資訊（可選，視前端需求決定是否回傳完整內容）
        public string? BestUsername { get; set; }             // 最佳解答作者暱稱

        public string? BestContentPreview { get; set; }       // 最佳解答前 N 字預覽（後端可截取）

        // 討論串相關（列表時可帶一點資訊）
        public string? RootContentTitle { get; set; }         // 討論串主標題（可選）

        public int? ReplyCount { get; set; }

    }
}
