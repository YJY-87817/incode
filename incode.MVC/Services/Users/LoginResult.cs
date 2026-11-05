
namespace incode.MVC.Services.Users
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }  // 登入是否成功
        public string Message { get; set; }  // 給前端/使用者看的訊息
        public User? Member { get; set; }    // 如果成功，這裡會有會員資料
        public bool IsLockedOut { get; set; } // 失敗次數過多，暫停登入（API 回 429）
    }
}
