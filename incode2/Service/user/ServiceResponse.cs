namespace incode2.Models
{
    public class ServiceResponse<T>
    {
        // 實際回傳的資料 (例如: "登入成功" 或 User 物件)
        public T? Data { get; set; }

        // 是否成功
        public bool Success { get; set; } = true;

        // 錯誤或提示訊息
        public string Message { get; set; } = string.Empty;
    }
}
