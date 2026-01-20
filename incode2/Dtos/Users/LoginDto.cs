using System.ComponentModel.DataAnnotations;

namespace incode2.Dtos.Users
{
    public class LoginDto
    {
        [Required(ErrorMessage = "請輸入Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; }

        [Display(Name = "記住我")]
        public bool RememberMe { get; set; }
    }
}
