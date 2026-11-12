namespace incode.MVC.Dtos.Chat
{
  /// <summary>聊天訊息的長度上限（訊息會整段送給 Gemini，太長會增加費用）</summary>
  public static class ChatLimits
  {
    public const int MaxMessageLength = 2000;

    /// <summary>SignalR 連線 ID（實際約 22 字元）</summary>
    public const int MaxConnectionIdLength = 100;
  }
}
