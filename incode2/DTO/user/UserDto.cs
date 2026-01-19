using System.ComponentModel.DataAnnotations;

namespace incode2.DTO.user
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string ImageUrl { get; set; }
        //public string Birthday { get; set; }     
        public string Role { get; set; }
    }


}
