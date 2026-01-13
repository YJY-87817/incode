using incode2.Models;

namespace incode2.DTO
{
    public class OrderDTO
    {
        public List<Orderdetail> orderdetails { get; set; }
        public int UserId { get; set; }

        public int TotalPoint { get; set; }

        public string Remark { get; set; }

        public string Status { get; set; }

        public int OrderId { get; set; }
    }
}
