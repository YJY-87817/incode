namespace incode2.Dtos.Point 
{
    public class UserMissionViewDTO
    {
        // 來自 Mission 的基本資訊
        public int MissionProgressId { get; set; }
        public int MissionId { get; set; }
        public string MissionName { get; set; }
        public string MissionDetail { get; set; }
        public string TargetType { get; set; }
        public int TargetCount { get; set; }
        public int RewardPoints { get; set; }
        public int RewardExp { get; set; }
        public string Category { get; set; }
        public string Difficulty { get; set; }
        public string EstimatedDuration { get; set; }
        public string ResetType { get; set; }  // "daily", "weekly", "none" 等
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }

        // 來自 MissionProgress 的個人進度
        public int CurrentCount { get; set; }
        public string Status { get; set; }  // "ongoing", "completed", "rewarded" 等
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? RewardedAt { get; set; }

        // 計算屬性（方便前端顯示）
        public double ProgressPercentage { get; set; }  // 0~100
        public bool CanClaimReward { get; set; }        // 是否已達標且可領獎勵（可選，先不計算）
        public string DisplayStatus { get; set; }       // 中文化狀態，例如 "進行中"、"已完成"、"已領取"
    }
}
