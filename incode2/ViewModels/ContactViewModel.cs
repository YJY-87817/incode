using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
  public class ContactViewModel
  {
    [Required(ErrorMessage ="記得填姓名")]
    public string Name { get; set; }
    [EmailAddress(ErrorMessage ="Email格式錯誤")]
    public string? Email { get; set; }
    public string? Phone { get; set; }
  }
}