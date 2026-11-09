using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "請輸入 Email")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "請輸入 Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "重設連結無效")]
        public string Token { get; set; }

        // 與註冊（RegisterDto）相同的密碼規則
        [Required(ErrorMessage = "請輸入新密碼")]
        [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = "密碼至少需要 6 碼")]
        public string NewPassword { get; set; }
    }

    public static class PasswordRules
    {
        public const int MinLength = 6;
        public const int MaxLength = 100;
    }
}
