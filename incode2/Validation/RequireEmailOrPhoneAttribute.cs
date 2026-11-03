using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace incode2.Validation
{
  //[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class RequireEmailOrPhoneAttribute : ValidationAttribute, IClientModelValidator
  {
    public void AddValidation(ClientModelValidationContext context)
    {
      context.Attributes.Add("data-val", "true");
      context.Attributes.Add("data-val-emailorphone", ErrorMessage);
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
      var model = (ContactViewModel)validationContext.ObjectInstance;
      if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Phone))
      {
        return new ValidationResult(ErrorMessage ?? "Either Email or Phone is required.",
            new[] { nameof(model.Email), nameof(model.Phone) }
        );
      }

      return ValidationResult.Success;
    }
  }
}
