using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Contents
{
  // 建立內容 Request DTO（客戶端傳送的資料）
  // 只包含建立內容時需要的欄位
  public class CreateContentDto
  {
    public int? ParentContentId { get; set; }
    public int? CategoryId { get; set; }
    public int UserId { get; set; }
    [StringLength(ContentLimits.MaxTitleLength)]
    public string Title { get; set; }
    [StringLength(ContentLimits.MaxBodyLength)]
    public string Body { get; set; }
    public int Type { get; set; }
  }
}
