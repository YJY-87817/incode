namespace incode2.DTO
{
    public class ShippedDTO
    {
        public DateTime? ShippedDate { get; set; }

        public DateTime? ArrivalDate { get; set; }

        public bool IsFailed { get; set; }

        public string Remark { get; set; }

        public bool IsCompleted { get; set; }

        public int OrderdetailId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

    }
}
