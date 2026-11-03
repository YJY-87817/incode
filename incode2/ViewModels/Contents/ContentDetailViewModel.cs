namespace incode2.ViewModels.Contents;

// Models/ViewModels/ContentDetailViewModel.cs
public class ContentDetailViewModel
{
  // 主貼文
  public int ContentId { get; set; }
  public string Title { get; set; }
  public string Body { get; set; }
  public DateTime CreatedAt { get; set; }
  public string AuthorName { get; set; }
  public string CategoryName { get; set; }

  // 回覆列表
  public List<ReplyViewModel> Replies { get; set; }
}

// 回覆項目
public class ReplyViewModel
{
  public int ContentId { get; set; }
  public string Body { get; set; }
  public string AuthorName { get; set; }
  public DateTime CreatedAt { get; set; }
}