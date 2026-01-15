namespace incode2.Dtos
{
  /// <summary>
  /// 建立內容 Request DTO（客戶端傳送的資料）
  /// 只包含建立內容時需要的欄位
  /// </summary>
  public class CreateContentDto
  {
    public int? ParentContentId { get; set; }
    public int? CategoryId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
  }
}
