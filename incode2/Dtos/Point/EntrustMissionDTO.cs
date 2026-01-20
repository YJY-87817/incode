namespace incode2.DTO
{
    public class EntrustMissionDTO
    {
        public int EntrustMissionId { get; set; }

        public int UserId { get; set; }

        public int ContentId { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public int RewardPoints { get; set; }

        public int MaxParticipants { get; set; }

        public string Status { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}