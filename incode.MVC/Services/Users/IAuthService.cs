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

        /// <summary>會員修改密碼（需目前的密碼）；成功後其他裝置登出，回傳的 User 用來重新簽發目前裝置的 Cookie。</summary>
        Task<(ChangePasswordResult Result, User? User)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>寄 Email 驗證信（連結為 verifyLinkBaseUrl?email=…&amp;token=…）。</summary>
        Task<SendVerificationResult> SendVerificationEmailAsync(int userId, string verifyLinkBaseUrl);

        Task<bool> VerifyEmailAsync(string email, string token);

        /// <summary>Google 登入：找不到就建立；接管同 Email、尚未驗證的密碼帳號時 TakenOver = true。</summary>
        Task<(User User, bool TakenOver)> GetOrCreateGoogleUserAsync(string email, string? name);

        /// <summary>後台替會員重設密碼：換新登入戳記（所有裝置登出）；Google 帳號不能設定密碼</summary>
        Task<ChangePasswordResult> AdminSetPasswordAsync(int userId, string newPassword);
    }

    public enum ChangePasswordResult
    {
        Success,
        NotFound,
        GoogleAccount,
        LockedOut,
        InvalidNewPassword,
        WrongPassword
    }

    public enum SendVerificationResult
    {
        Sent,
        AlreadyVerified,
        NotFound
    }
}
