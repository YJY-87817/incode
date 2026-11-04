namespace incode.MVC.Dtos.PointShop
{
    public class ProductDTO
    {
        public int ProductId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageName { get; set; }

        public string Type { get; set; }

        public string Series { get; set; }

        public bool IsPhysical { get; set; }

        public int Point { get; set; }

        public int Stock { get; set; }

    }
}
