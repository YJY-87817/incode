using System.ComponentModel.DataAnnotations;

namespace incode2.Metadata.Contents
{
  internal class ContentCategoryMetadata
  {
    [Required(ErrorMessage = "種類名稱未填寫")]
    [StringLength(maximumLength: 50, MinimumLength = 2, ErrorMessage = "種類名稱最少2個字")]
    [Display(Name="種類名稱")]
    public string CategoryName { get; set; }

  }
}