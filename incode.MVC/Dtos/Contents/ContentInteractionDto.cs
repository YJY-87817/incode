namespace incode.MVC.Dtos.Contents
{
  public class ContentInteractionDto
  {
    public int ContentInteractionId { get; set; }

    public int InteractionCategoryId { get; set; }

    public string InteractionCategoryName { get; set; }

    public int ContentId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }
  }
}
