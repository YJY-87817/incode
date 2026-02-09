namespace incode2.Dtos
{
  /// <summary>
  /// 更新內容 Request DTO（客戶端傳送的資料）
  /// 只包含可以更新的欄位
  /// </summary>
  public class UpdateContentDto
  {
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime? UpdatedAt { get; set; }
  }
}
