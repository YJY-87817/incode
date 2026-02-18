using incode2.Dtos.Users;
using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class MemberEditViewModel : MemberIndexViewModel
    {
        [Display(Name = "重設密碼")]
        [MinLength(6, ErrorMessage = "密碼最少6碼")]
        public string? Password { get; set; }

        [Display(Name = "帳號狀態")]
        public bool IsActive { get; set; } // 控制啟用/停權

        // 只讀的登入紀錄 (用於顯示異常給管理員看)
        public List<LoginLogDto> RecentLoginLogs { get; set; } = new List<LoginLogDto>();
    }
}