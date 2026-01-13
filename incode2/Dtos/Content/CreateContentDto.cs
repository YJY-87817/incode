using Microsoft.AspNetCore.Mvc;

namespace incode2.Dtos
{
  public class ContentDto
  {
    public int ContentId { get; set; }
    public int? ParentContentId { get; set; }
    public int UserId { get; set; }
    public string CategoryName { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? Ishidden { get; set; }
    public string Username { get; set; }
  }
}
