using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class MemberBaseViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "使用者")]
        [Required(ErrorMessage = "請輸入姓名")]
        public string? Username { get; set; }

        [Display(Name = "電子郵件(帳號)")]
        [Required(ErrorMessage = "請輸入Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; }

        public string Password { get; set; }
    }
}