namespace incode2.Dtos
{
  public class CreateContentInteractionDto
  {
    /// <summary>
    /// 互動分類 ID (like=1、dislike=2、block=3、bookmark=4 等)
    /// </summary>
    public int InteractionCategoryId { get; set; }

    /// <summary>
    /// 內容 ID
    /// </summary>
    public int ContentId { get; set; }

    /// <summary>
    /// 使用者 ID（從登入狀態取得）
    /// </summary>
    public int UserId { get; set; }
  }
}
