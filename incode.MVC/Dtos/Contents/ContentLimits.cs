namespace incode.MVC.Dtos.Contents
{
  /// <summary>論壇文章欄位的長度上限（DTO 以 [StringLength] 檢查，前端輸入框的 maxlength 也用同樣的值）</summary>
  public static class ContentLimits
  {
    /// <summary>標題：資料庫欄位 content.title 為 nvarchar(200)</summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// 內文 HTML（存成檔案）：500 萬字元。圖片上傳失敗時編輯器會改以 base64 內嵌圖片，所以不能設得太小；
    /// 請求大小另受全站上限（Program.cs，30 MB）限制
    /// </summary>
    public const int MaxBodyLength = 5_000_000;
  }
}
