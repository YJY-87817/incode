using System;
using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class PointLogsViewModel
    {
        [Display(Name = "時間")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "異動點數")]
        public int ChangeAmount { get; set; }

        [Display(Name = "來源說明")]
        public string Description { get; set; } = string.Empty;

        // --- 修正：補上缺失的 DisplayName 定義 ---
        [Display(Name = "類別")]
        public string DisplayName { get; set; } = string.Empty; // 儲值、任務獎勵等
        // ----------------------------------------

        [Display(Name = "餘額")]
        public int NewBalance { get; set; }

        // --- 顯示邏輯 (View 使用) ---

        // 格式化異動金額：例如 +1,000 或 -500 (包含千分位)
        public string FormattedChangeAmount => ChangeAmount >= 0
            ? $"+{ChangeAmount:N0}"
            : $"{ChangeAmount:N0}";

        // 格式化餘額：例如 5,000
        public string FormattedNewBalance => $"{NewBalance:N0}";

        // 判斷顏色：正數綠色，負數紅色
        public string ChangeAmountCss => ChangeAmount >= 0 ? "text-success" : "text-danger";
    }
}