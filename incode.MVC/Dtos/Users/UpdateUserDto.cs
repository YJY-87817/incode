using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class UpdateUserDto
    {
        [Required]
        public string Email { get; set; }
        public string? Username { get; set; }
        public string? ImageUrl { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateOnly? Birthday { get; set; }
    }
}
