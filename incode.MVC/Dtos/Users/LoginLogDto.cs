using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class LoginLogDto
    {
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; }        
        public bool IsSuspicious { get; set; }
    }
}
