namespace incode2.Dtos
{
  /// <summary>
  /// 內容 Response DTO（用於回傳資料給客戶端）
  /// 包含完整的內容資訊，包括ID、時間戳、使用者資訊等
  /// </summary>
  public class ContentDto
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
    public DateTime? UpdatedAt { get; set; }
    public bool? Ishidden { get; set; }
  }
}
