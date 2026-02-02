using incode2.Models;

namespace incode2.Services.Users
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }  // 登入是否成功
        public string Message { get; set; }  // 給前端/使用者看的訊息
        public User? Member { get; set; }    // 如果成功，這裡會有會員資料
    }
}
