using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
  public class ContactViewModel : IValidatableObject
  {
    [Required(ErrorMessage = "記得填姓名")]
    public string Name { get; set; }
    [EmailAddress(ErrorMessage = "Email格式錯誤")]
    public string? Email { get; set; }
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