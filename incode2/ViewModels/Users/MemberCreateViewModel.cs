using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class MemberCreateViewModel : MemberBaseViewModel
    {
        [Display(Name = "密碼")]
        [Required(ErrorMessage = "請設定密碼")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼最少6碼")]
        public string Password { get; set; }

        [Display(Name = "確認密碼")]
        [Compare("Password", ErrorMessage = "兩次密碼輸入不一致")]
        public string ConfirmPassword { get; set; }
    }
}