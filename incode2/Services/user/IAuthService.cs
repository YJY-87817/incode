using incode2.Models;

namespace incode2.Services.user
{
    public interface IAuthService
    {
        // 1. 驗證使用者 (登入用) 
        Task<User?> ValidateUserAsync(string email, string password);

        // 2. 註冊使用者 
        Task<string> RegisterAsync(string email, string password);
    }
}
