using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace incode2.Data
{
  public class ApplicationUser: IdentityUser
  {
        //[MaxLength(3)]
        //public string CountryCode { get; set; }
        public string? Account { get; set; }
    }
}
