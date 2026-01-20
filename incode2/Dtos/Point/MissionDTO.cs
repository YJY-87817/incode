namespace incode2.DTO
{
    public class MissionDTO
    {
        public int MissionId { get; set; }

        public string MissionName { get; set; }

        public string MissionDetail { get; set; }

        public string TargetType { get; set; }

        public int TargetCount { get; set; }

        public int RewardPoints { get; set; }

        public int RewardExp { get; set; }

        public string ResetType { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}