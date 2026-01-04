using System.ComponentModel.DataAnnotations;

namespace incode2.Metadata
{
  internal class ContentCategoryMetadata
  {
    [Display(Name="種類名稱")]
    public string CategoryName { get; set; }

  }
}