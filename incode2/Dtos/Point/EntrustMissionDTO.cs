using System.ComponentModel.DataAnnotations;

namespace incode2.DTO
{
    public class EntrustMissionDTO
    {
        public int? EntrustMissionId { get; set; }  // 查詢/更新時用，新增時可 null

        [Required]
        public int UserId { get; set; }             // 發起委託的使用者

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
        [Range(1, 99, ErrorMessage = "最多參與人數必須至少 1")]
        public int MaxParticipants { get; set; } = 1;

        public int CurrentParticipants { get; set; } = 0;  // 改成非 nullable，預設 0

        [Required]
        public string Status { get; set; } = "open";

        public bool IsUrgent { get; set; } = false;        // 改成非 nullable

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }           // 移除預設值，由後端設定

        public DateTime? UpdatedAt { get; set; }

    }
}