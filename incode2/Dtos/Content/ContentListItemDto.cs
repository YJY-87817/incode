namespace incode2.Dtos
{
  public class ContentListItemDto
  {
    public int ContentId { get; set; }
    public int? ParentContentId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public int? Type { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ReplyCount { get; set; }
    public DateTime? LastReplyTime { get; set; }
    public string LastReplyUser { get; set; }
    public int? LastReplyUserId { get; set; }
    public string PreviewText { get; set; }
    public List<string> PreviewImages { get; set; } = new();
    public string PreviewHtml { get; set; }
    public MyInteractionsDto MyInteractions { get; set; } = new();
  }
}
