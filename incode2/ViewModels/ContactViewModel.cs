using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
  public class ContactViewModel
  {
    [Required(ErrorMessage ="記得填姓名")]
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
  }
}