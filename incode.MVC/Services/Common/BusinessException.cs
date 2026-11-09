namespace incode.MVC.Services.Common
{
    /// <summary>
    /// 業務規則不允許的操作（例如點數不足、不能打賞自己）。Message 是給使用者看的訊息，Controller 可以直接回傳。
    /// 其他例外（資料庫、網路、程式錯誤）不要把 Message 回給前端：用 ILogger 記錄後回傳通用訊息。
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }
}
