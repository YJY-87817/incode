using System.ComponentModel.DataAnnotations;

namespace incode2.DTO.user
{
    public class LoginDto
    {
        [Required(ErrorMessage = "請輸入Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; }
    }
}
