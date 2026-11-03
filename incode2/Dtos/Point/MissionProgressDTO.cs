namespace incode2.Dtos.Point
{
    public class MissionProgressDTO
    {
        public int MissionProgressId { get; set; }

        public int UserId { get; set; }

        public int MissionId { get; set; }

        public int CurrentCount { get; set; }

        public string Status { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? RewardedAt { get; set; }
    }
}