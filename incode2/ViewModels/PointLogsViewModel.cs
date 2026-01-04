namespace incode2.ViewModels
{
    public class PointLogsViewModel
    {
        public DateTime CreatedAt { get; set; }                  // 時間
        public int ChangeAmount { get; set; }                     // 異動點數
        public string Description { get; set; } = string.Empty;  // 說明（任務名、訂單號）
        public string TypeDisplayName { get; set; } = string.Empty;
        public int NewBalance { get; set; }                       // 異動後餘額

        // 方便 View 判斷顏色
        public string ChangeAmountText => ChangeAmount > 0 ? $"+{ChangeAmount}" : ChangeAmount.ToString();
        public string ChangeAmountCss => ChangeAmount > 0 ? "text-success" : "text-danger";

    }
}