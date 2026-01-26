namespace incode2.Dtos
{
  public class CreateContentInteractionDto
  {
    // 互動分類 ID (like=1、dislike=2、block=3、bookmark=4 等)
    public int InteractionCategoryId { get; set; }

    // 內容 ID
    public int ContentId { get; set; }

    // 使用者 ID（從登入狀態取得）
    public int UserId { get; set; }
  }
}
