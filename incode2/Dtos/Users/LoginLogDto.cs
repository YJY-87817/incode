using System.ComponentModel.DataAnnotations;

namespace incode2.Dtos.Users
{
    public class LoginLogDto
    {
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; }        
        public bool IsSuspicious { get; set; }
    }
}
