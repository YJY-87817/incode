using System.ComponentModel.DataAnnotations;

namespace incode2.DTO.user
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email 格式不正確")] // 登入不需要這個檢查
        public string Email { get; set; } 

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼至少需要 8 碼")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")] // 這是註冊獨有的
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "暱稱不能超過 20 字元")]
        public string Username { get; set; }
    }
}
