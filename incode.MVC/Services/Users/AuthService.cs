using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Users
{
    public class AuthService : IAuthService
    {
        private readonly incodedatabaseContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IMissionService _missionService;
        private readonly LoginAttemptLimiter _loginLimiter;

        public AuthService(incodedatabaseContext context, IPasswordService passwordService, IEmailService emailService, IUserService userService, IMissionService missionService, LoginAttemptLimiter loginLimiter)
        {
            _loginLimiter = loginLimiter;
            _context = context;
            _passwordService = passwordService;
            _userService = userService;
            _emailService = emailService;
            _missionService = missionService;
        }
        public async Task<string> RegisterAsync(string email, string password, string? preferredUsername, string? imageUrl = null)
        {           
            if (await _userService.IsEmailExistAsync(email))
            {
                return "Email 已被註冊"; // 1. 檢查 Email (呼叫 UserService)
            } 
            
            string passwordHash = _passwordService.Hash(password);// 2. 加密密碼 (呼叫 PasswordService)      
            string generatedUsername = GenerateUsernameFromEmail(email);// 自動產生 Username (Email @ 前贅詞 + 2位亂碼)
                  
            string finalUsername;
            if (!string.IsNullOrWhiteSpace(preferredUsername))// 使用者自己填了
            { 
                if (await _userService.IsUsernameExistAsync(preferredUsername)) // 使用者填的 須檢查「是否已有人使用」
                {
                    return "此使用者名稱已被使用";
                }
                finalUsername = preferredUsername;
            }
            else
            {  
                finalUsername = GenerateUsernameFromEmail(email);// 使用者沒填 (null 或空) 執行原本的自動產生邏輯
            }            
            
            var newUser = new User // 3. 準備資料
            {
                Email = email,
                Username = finalUsername,
                ImageUrl = imageUrl,
                Password = passwordHash,
                CreatedAt = DateTime.Now,
                RoleId = 2 // 預設 Member
            };           
            await _userService.CreateUserAsync(newUser);  // 4. 存檔 (呼叫 UserService)

            return "Success";
        }
        private string GenerateUsernameFromEmail(string email)
        {           
            var prefix = email.Split('@')[0];// 取得 @ 前面的字串            
            var randomCode = GetRandomString(2);// 產生 2 位亂碼 (包含數字與大寫字母)
            return $"{prefix}{randomCode}";
        }
        private string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";            
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        //  登入驗證
        public async Task<LoginResult> ValidateUserAsync(string email, string password)
        {
            email ??= string.Empty;

            // 同一帳號失敗太多次：在比對密碼前就擋下（帳號是否存在都一樣處理，不透露帳號資訊）
            if (_loginLimiter.IsLockedOut(email))
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    IsLockedOut = true,
                    Message = $"登入失敗次數過多，請 {LoginAttemptLimiter.Window.TotalMinutes:0} 分鐘後再試"
                };
            }

            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                _loginLimiter.RecordFailure(email);
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "帳號或密碼錯誤"
                };
            }

            if (user.IsActive == false)  // 帳號明確被設為 false (停權)，擋下來
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "此帳號出現異常情況已被鎖定，請聯繫客服。"
                };
            }

            // 2. 驗證密碼
            bool isPasswordCorrect = _passwordService.Verify(password, user.Password);

            if (!isPasswordCorrect)
            {
                _loginLimiter.RecordFailure(email);
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "帳號或密碼錯誤"
                };
            }

            _loginLimiter.Reset(email);

            // 登入成功 → 觸發每日登入任務
            await _missionService.TriggerMissionAsync(user.UserId, "login", 1);

            return new LoginResult
            {
                IsSuccess = true,
                Message = "登入成功",
                Member = user
            };
        }
            // 1. 忘記密碼請求
    public async Task<bool> ForgotPasswordAsync(string email, string resetLinkBaseUrl)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false; // 為了安全，找不到也可以回傳 true，但這裡方便除錯先回傳 false

            // A. 產生 Token（密碼學亂數 32 bytes）
            string token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            // B. 存入資料庫 (設定 30 分鐘後過期)
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(30);

            _context.Update(user);
            await _context.SaveChangesAsync();

            // C. 組合重設連結：email 要 URL 編碼（含 + 或 & 的信箱原本會變成錯的參數），放進 HTML 前再編碼
            string resetUrl = $"{resetLinkBaseUrl}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            // D. 寄信
            string emailContent = $"<h3>重設密碼通知</h3><p>請點擊以下連結重設您的密碼（30 分鐘內有效）：</p><a href=\"{System.Net.WebUtility.HtmlEncode(resetUrl)}\">重設密碼</a>";
            await _emailService.SendEmailAsync(email, "重設密碼通知", emailContent);

            return true;
        }

        // 2. 執行重設密碼
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return false;

            // 新密碼規則與註冊相同（Controller 的 DTO 也會檢查）
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < PasswordRules.MinLength || newPassword.Length > PasswordRules.MaxLength)
                return false;

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

            // 驗證: 找不到人、沒有申請過重設（Token / 期限為 null）、Token 不對、Token 過期
            // （原本 ResetToken 為 null 時，只要送出 null token 就能通過比對，靠 [ApiController] 的必填檢查才擋住）
            if (user == null
                || string.IsNullOrEmpty(user.ResetToken)
                || user.ResetTokenExpiry == null
                || user.ResetTokenExpiry < DateTime.Now
                || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                       System.Text.Encoding.UTF8.GetBytes(user.ResetToken),
                       System.Text.Encoding.UTF8.GetBytes(token)))
            {
                return false;
            }

            // A. 加密新密碼 (使用您原本的 BCrypt 邏輯)
            // 假設您的 PasswordService 有 HashPassword 方法
            string hashedPassword = _passwordService.Hash(newPassword);
            user.Password = hashedPassword; // 更新密碼欄位

            // B. 清除 Token (避免重複使用)
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            _context.Update(user);
            await _context.SaveChangesAsync();

            // 密碼已重設，解除登入失敗造成的暫停
            _loginLimiter.Reset(email);

            return true;
        }
    }
           
}
