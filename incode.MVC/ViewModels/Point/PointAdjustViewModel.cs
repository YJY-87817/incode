using System.ComponentModel.DataAnnotations;

namespace incode.MVC.ViewModels.Point
{
    /// <summary>後台手動調整點數（PointLog/Adjust）。規則在 PointLogService.AdminAdjustPointsAsync 檢查。</summary>
    public class PointAdjustViewModel
    {
        [Display(Name = "會員")]
        [Range(1, int.MaxValue, ErrorMessage = "請選擇會員")]
        public int UserId { get; set; }

        [Display(Name = "調整點數（扣點填負數）")]
        [Range(-PointLogService.MaxAdminAdjustment, PointLogService.MaxAdminAdjustment, ErrorMessage = "調整點數需在 ±100,000 之間")]
        public int ChangeAmount { get; set; }

        [Display(Name = "原因（會顯示在會員的點數紀錄）")]
        [Required(ErrorMessage = "請填寫調整原因")]
        [StringLength(PointLogService.MaxAdjustReasonLength)]
        public string? Reason { get; set; }
    }
}
