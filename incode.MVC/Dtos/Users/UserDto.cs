using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class UserDto
    {
        public int UserId { get; set; }       
        public string Username { get; set; }
        public string ImageUrl { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Phone { get; set; }   
        public string? Address { get; set; }
        public DateOnly? Birthday { get; set; }

        // 以下只回給本人（GET api/User/profile 看自己時）
        public bool? EmailVerified { get; set; }
        public bool? IsGoogleAccount { get; set; }
    }


}
