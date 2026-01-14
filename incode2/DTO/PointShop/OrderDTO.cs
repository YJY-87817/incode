namespace incode2.DTO
{
    public class OrderDTO
    {
        public int UserId { get; set; }

        public int TotalPoint { get; set; }

        public string Remark { get; set; }

        public string Status { get; set; }

        public int OrderId { get; set; }
    }

    public class OrderDetailDTO
    {
        public int OrderdetailId { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ImageName { get; set; }

        public int Quantity { get; set; }

        public int UnitPointAtPurchase { get; set; }

        public bool RequiresShipping { get; set; }
    }
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
