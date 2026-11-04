namespace incode.MVC.Constants
{
    /// <summary>
    /// 點數商城訂單狀態 (需與後台下拉選單、前端一致)
    /// </summary>
    public static class OrderStatus
    {
        public const string Pending = "待處理";
        public const string Shipped = "已出貨";
        public const string Completed = "已完成";
        public const string Canceled = "已取消";
        public const string Other = "其他";

        public static readonly string[] All = { Pending, Shipped, Completed, Canceled, Other };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
