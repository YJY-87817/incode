using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
  public class ContactViewModel : IValidatableObject
  {
    [Required(ErrorMessage = "記得填姓名")]
    [Display(Name = "姓名")]
    [StringLength(maximumLength: 8, MinimumLength = 2, ErrorMessage = "姓名最少2個字")]
    public string Name { get; set; }

    [EmailAddress(ErrorMessage = "Email格式錯誤")]
    [Display(Name = "電子信箱")]
    public string? Email { get; set; }

    [Display(Name = "電話")]
    public string? Phone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(Phone))
      {
        yield return new ValidationResult("2個選1個填");
      }
    }
  }
}