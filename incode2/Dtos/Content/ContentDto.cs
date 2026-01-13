using Microsoft.AspNetCore.Mvc;

namespace incode2.Dtos
{
  // DTOs
  public class CreateContentDto
  {
    public int? ParentContentId { get; set; }
    public int? CategoryId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
  }
}
