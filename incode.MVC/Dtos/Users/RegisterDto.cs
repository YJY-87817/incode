using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } 

        [Required]
        [StringLength(PasswordRules.MaxLength, MinimumLength = PasswordRules.MinLength, ErrorMessage = "密碼至少需要 6 碼")]
        public string Password { get; set; }

        public string? Username { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? ImageUrl { get; set; }
    }
}
