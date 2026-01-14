namespace incode2.DTO
{
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
}
