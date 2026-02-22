namespace incode2.DTO
{
    public class EntrustMissionProgressDTO
    {
        public int EntrustMissionProgressId { get; set; }

        public int UserId { get; set; }

        public int EntrustMissionId { get; set; }

        public string Status { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? RewardedAt { get; set; }

        // --- 新增以下欄位，對應 Vue 畫面需求 ---
        public string Title { get; set; }        // 任務標題
        public string Detail { get; set; }       // 任務內容
        public string Category { get; set; }     // 任務分類
        public int RewardPoints { get; set; }    // 賞金點數
    }
}
