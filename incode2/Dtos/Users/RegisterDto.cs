using System.ComponentModel.DataAnnotations;

namespace incode2.Dtos.Users
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } 

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼至少需要 6 碼")]
        public string Password { get; set; }

        [Required(ErrorMessage = "請再次輸入密碼")]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
        public string ConfirmPassword { get; set; }
    }
}
