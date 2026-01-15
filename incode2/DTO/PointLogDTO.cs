namespace incode2.DTO
{
    public class PointLogDTO
    {
        public int PointLogId { get; set; }

        public int UserId { get; set; }

        public string SourceType { get; set; }

        public int SourceId { get; set; }

        public int PointTypeDefinitionId { get; set; }

        public int ChangeAmount { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}