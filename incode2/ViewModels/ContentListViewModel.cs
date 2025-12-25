namespace incode2.Models.ViewModels
{
  public class ContentListViewModel
  {
    // 貼文資料
    public int ContentId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime CreatedAt { get; set; }

    // 作者資料
    public string AuthorName { get; set; }

    // 分類名稱
    public string CategoryName { get; set; }
  }
}