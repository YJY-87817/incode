using System.ComponentModel;

namespace incode.MVC.ViewModels.Contents
{
  public class ContentViewModel
  {
    // 貼文資料
    [DisplayName("內容編號")]
    public int ContentId { get; set; }
    [DisplayName("主文ID")]
    public int? ParentContentId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime CreatedAt { get; set; }
    // 作者資料
    public string AuthorName { get; set; }

    // 分類名稱
    public string CategoryName { get; set; }
  }
}