namespace incode.MVC.Dtos.Point
{
    /// <summary>會員查詢自己的儲值訂單狀態（付款完成頁用）。</summary>
    public class RechargeOrderStatusDto
    {
        public string OrderNo { get; set; } = "";
        public string? Status { get; set; }
        public decimal CashAmount { get; set; }
        public int PointsGranted { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
