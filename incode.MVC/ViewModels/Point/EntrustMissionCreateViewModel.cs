using System.ComponentModel.DataAnnotations;

namespace incode.MVC.ViewModels.Point
{
    /// <summary>
    /// 後台代會員建立委託（EntrustMission/Create）：選擇會員的主題文章，賞金從該文章作者的點數預扣
    /// （與前台相同，走 EntrustMissionService.CreateEntrustMissionAsync）。
    /// </summary>
    public class EntrustMissionCreateViewModel
    {
        [Display(Name = "主題文章（委託人為文章作者）")]
        [Range(1, int.MaxValue, ErrorMessage = "請選擇主題文章")]
        public int ContentId { get; set; }

        [Display(Name = "任務標題")]
        [Required(ErrorMessage = "請填寫任務標題")]
        [StringLength(150)]
        public string Title { get; set; } = "";

        [Display(Name = "任務詳情")]
        [Required(ErrorMessage = "請填寫任務詳情")]
        [StringLength(500)]
        public string Detail { get; set; } = "";

        [Display(Name = "賞金（從作者點數預扣）")]
        [Range(1, int.MaxValue, ErrorMessage = "賞金必須大於 0")]
        public int RewardPoints { get; set; }

        [Display(Name = "類別")]
        [Required(ErrorMessage = "請填寫類別")]
        [StringLength(50)]
        public string Category { get; set; } = "其他";

        [Display(Name = "截止日期")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "緊急")]
        public bool IsUrgent { get; set; }
    }
}
