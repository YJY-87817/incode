using Humanizer;

namespace incode.MVC.Services.Users
{
    public interface IAuthService
    {
        // 1. 驗證使用者 (登入用) 
        Task<LoginResult> ValidateUserAsync(string email, string password);

        // 2. 註冊使用者 
        Task<string> RegisterAsync(string email, string password, string Username, string? imageUrl = null);

        Task<bool> ForgotPasswordAsync(string email, string resetLinkBaseUrl);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
