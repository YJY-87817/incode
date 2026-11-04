namespace incode.MVC.Dtos.Point
{
    public class PointLogDTO
    {
        public int PointLogId { get; set; }

        public int UserId { get; set; }

        public string SourceType { get; set; }

        public int? SourceId { get; set; }

        public string? DisplaySource { get; set; }

        public string? DisplaySourceSubTitle { get; set; }

        public int PointTypeDefinitionId { get; set; }

        public int ChangeAmount { get; set; }

        public int BalanceAfter {  get; set; }

        public string Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ← 加上這一行！
        public string DisplayName { get; set; } = string.Empty;
    }
}
