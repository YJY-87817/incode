
namespace incode2.Dtos.PointShop
{
    // 1. 給前端傳入用的 (Input)
    public class OrderRequest
    {
        public int UserId { get; set; }
        public string? Remark { get; set; }
        // 這裡的 Detail 只需要 ID 跟數量
        public List<OrderItemRequest> Items { get; set; }

        public NewShippedRequest Recipient { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // 2. 原本的 DTO，可以用來做 API 的回傳結果 (Output)
    public class NewOrderDTO
    {
        public List<Orderdetail> orderdetails { get; set; }
        public int UserId { get; set; }
        public int TotalPoint { get; set; } // 這是由後端計算後填入的
        public string Remark { get; set; }
        public string Status { get; set; }
        public int OrderId { get; set; }
    }

    public class NewShippedRequest
    { 
        public int OrderId { get; set; }

        public string RecipientName { get; set; }

        public string RecipientAddress { get; set; }

        public string RecipientPhone { get; set; }

        public string? Remark { get; set; } 
    }
}
