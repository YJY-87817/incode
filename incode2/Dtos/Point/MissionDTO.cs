namespace incode2.Dtos.Point
{
    public class MissionDTO
    {
        public int? MissionId { get; set; }

        public string MissionName { get; set; }

        public string MissionDetail { get; set; }

        public string TargetType { get; set; }

        public int TargetCount { get; set; }

        public int RewardPoints { get; set; }

        public int RewardExp { get; set; }

        public string Category { get; set; }

        public string Difficulty { get; set; }

        public string EstimatedDuration { get; set; }

        public string ResetType { get; set; }

        public int? ParticipantCount { get; set; }

        public bool? IsFeatured { get; set; }

        public DateTime? LastResetAt { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

    }

    public class ClaimMissionRequest
    {
        public int MissionId { get; set; }
    }
}