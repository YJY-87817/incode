using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Contents
{
  /// <summary>
  /// 更新內容 Request DTO（客戶端傳送的資料）
  /// 只包含可以更新的欄位
  /// </summary>
  public class UpdateContentDto
  {
    [StringLength(ContentLimits.MaxTitleLength)]
    public string Title { get; set; }
    [StringLength(ContentLimits.MaxBodyLength)]
    public string Body { get; set; }
    public DateTime? UpdatedAt { get; set; }
  }
}
