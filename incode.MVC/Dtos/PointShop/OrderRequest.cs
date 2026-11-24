
using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.PointShop
{
    // 1. 給前端傳入用的 (Input)
    public class OrderRequest
    {
        public int UserId { get; set; }
        [StringLength(OrderLimits.MaxRemarkLength)]
        public string? Remark { get; set; }
        // 這裡的 Detail 只需要 ID 跟數量
        public List<OrderItemRequest> Items { get; set; }

        public NewShippedRequest Recipient { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        [Range(1, OrderLimits.MaxQuantityPerItem)]
        public int Quantity { get; set; }
    }

    // 2. 原本的 DTO，可以用來做 API 的回傳結果 (Output)
    public class NewShippedRequest
    { 
        public int OrderId { get; set; }

        [StringLength(OrderLimits.MaxRecipientLength)]
        public string RecipientName { get; set; }

        [StringLength(OrderLimits.MaxRecipientLength)]
        public string RecipientAddress { get; set; }

        [StringLength(OrderLimits.MaxRecipientLength)]
        public string RecipientPhone { get; set; }

        [StringLength(OrderLimits.MaxRemarkLength)]
        public string? Remark { get; set; } 
    }

    /// <summary>商城訂單的上限（第三次全專案檢查 51；長度對照 orders / shipped_detail 的欄位）</summary>
    public static class OrderLimits
    {
        /// <summary>同一商品一筆訂單最多的數量（同商品多筆明細合併後計算）</summary>
        public const int MaxQuantityPerItem = 1000;
        public const int MaxRecipientLength = 50;   // shipped_detail.recipient_name / address / phone nvarchar(50)
        public const int MaxRemarkLength = 300;     // orders.remark、shipped_detail.remark nvarchar(300)
    }
}
