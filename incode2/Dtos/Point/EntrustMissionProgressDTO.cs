namespace incode2.DTO
{
    public class EntrustMissionProgressDTO
    {
        public int EntrustMissionProgressId { get; set; }

        public int UserId { get; set; }

        public int EntrustMissionId { get; set; }

        public string Status { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? RewardedAt { get; set; }
    }
}