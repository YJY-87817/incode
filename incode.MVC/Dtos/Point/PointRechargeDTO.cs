using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Point
{
    /// <summary>
    /// 前台建立儲值訂單（POST api/PointRechargeApi/recharge）。
    /// 原本繼承 PointRecharge 實體且金額沒有驗證，0、負數、超大值（點數 = 金額 × 10 會溢位成負數）都能建立訂單
    /// </summary>
    public class PointRechargeDTO
    {
        public const int MinCashAmount = 10;
        public const int MaxCashAmount = 50000;

        [Range(MinCashAmount, MaxCashAmount, ErrorMessage = "儲值金額必須介於 {1} 到 {2} 元")]
        public int CashAmount { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }
    }
}
