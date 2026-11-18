using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly IMemoryCache _cache;

        /// <summary>Email 驗證信的有效時間</summary>
        public static readonly TimeSpan EmailVerifyTokenLifetime = TimeSpan.FromHours(24);

        public AuthService(incodedatabaseContext context, IPasswordService passwordService, IEmailService emailService, IUserService userService, IMissionService missionService, LoginAttemptLimiter loginLimiter, IMemoryCache cache)
        {
            _loginLimiter = loginLimiter;
            _cache = cache;
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

            // 大頭貼只接受站內上傳的圖片（POST api/User/images 回傳的網址）
            if (!AvatarUrls.IsAllowed(imageUrl))
            {
                return "大頭貼請使用上傳的圖片";
            }
            
            string passwordHash = _passwordService.Hash(password);// 2. 加密密碼 (呼叫 PasswordService)      
            string generatedUsername = GenerateUsernameFromEmail(email);// 自動產生 Username (Email @ 前贅詞 + 2位亂碼)
                  
            string finalUsername;
            if (!string.IsNullOrWhiteSpace(preferredUsername))// 使用者自己填了
            { 
                preferredUsername = preferredUsername.Trim();
                if (!UsernameRules.IsValid(preferredUsername))
                {
                    return UsernameRules.ErrorMessage;
                }
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
            // 取得 @ 前面的字串，依暱稱規則處理（移除 < > 與控制字元，加上亂碼後不超過 30 字）
            var prefix = UsernameRules.Sanitize(email.Split('@')[0], "member", UsernameRules.MaxLength - 2);
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

            // Google 帳號沒有密碼（密碼欄是標記字串，原本交給 BCrypt 驗證會丟例外 → 500）
            if (user.Password == UserService.GoogleAccountPassword)
            {
                _loginLimiter.RecordFailure(email);
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "此帳號使用 Google 登入，請點選「使用 Google 登入」"
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

            // Google 帳號沒有密碼、也不提供設定密碼：不產生重設連結（原本可以用「忘記密碼」替 Google 帳號設定密碼），
            // 改寄一封提醒信。API 回應與一般帳號相同，不會透露帳號類型
            if (user.Password == UserService.GoogleAccountPassword)
            {
                await _emailService.SendEmailAsync(email, "重設密碼通知",
                    "<h3>重設密碼通知</h3><p>您的帳號使用 Google 登入，沒有密碼可以重設。請在登入頁點選「使用 Google 登入」。</p>");
                return true;
            }

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

            // C. 換新登入戳記：所有裝置的登入失效（原本重設密碼後舊的登入 Cookie 仍然有效）
            user.SecurityStamp = NewSecurityStamp();

            // 能收到重設信，代表擁有這個 Email
            user.EmailVerified = true;

            _context.Update(user);
            await _context.SaveChangesAsync();
            MemberCookieValidator.Evict(_cache, user.UserId);

            // 密碼已重設，解除登入失敗造成的暫停
            _loginLimiter.Reset(email);

            return true;
        }

        /// <summary>新的登入戳記（換新後，帶著舊戳記的登入 Cookie 會在下一個請求被登出）。</summary>
        public static string NewSecurityStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        private static bool TokenEquals(string expected, string actual) =>
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(expected),
                System.Text.Encoding.UTF8.GetBytes(actual));

        // 3. 會員修改密碼：要輸入目前的密碼。成功後換新登入戳記（其他裝置登出），回傳的 User 給 Controller 重新簽發目前裝置的 Cookie
        public async Task<(ChangePasswordResult Result, User? User)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return (ChangePasswordResult.NotFound, null);

            // Google 帳號沒有密碼，也不提供設定密碼
            if (user.Password == UserService.GoogleAccountPassword) return (ChangePasswordResult.GoogleAccount, null);

            // 與登入共用失敗次數限制，避免拿到別人的登入狀態後猜目前的密碼
            if (_loginLimiter.IsLockedOut(user.Email)) return (ChangePasswordResult.LockedOut, null);

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < PasswordRules.MinLength || newPassword.Length > PasswordRules.MaxLength)
                return (ChangePasswordResult.InvalidNewPassword, null);

            if (!_passwordService.Verify(currentPassword ?? "", user.Password))
            {
                _loginLimiter.RecordFailure(user.Email);
                return (ChangePasswordResult.WrongPassword, null);
            }

            user.Password = _passwordService.Hash(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.SecurityStamp = NewSecurityStamp();
            await _context.SaveChangesAsync();

            MemberCookieValidator.Evict(_cache, user.UserId);
            _loginLimiter.Reset(user.Email);
            return (ChangePasswordResult.Success, user);
        }

        // 4. 寄 Email 驗證信（註冊後、會員中心重寄）。寄信失敗的例外由呼叫端記錄
        public async Task<SendVerificationResult> SendVerificationEmailAsync(int userId, string verifyLinkBaseUrl)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return SendVerificationResult.NotFound;
            if (user.EmailVerified) return SendVerificationResult.AlreadyVerified;

            string token = NewToken();
            user.EmailVerifyToken = token;
            user.EmailVerifyTokenExpiry = DateTime.Now.Add(EmailVerifyTokenLifetime);
            await _context.SaveChangesAsync();

            string verifyUrl = $"{verifyLinkBaseUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            string emailContent = "<h3>驗證您的 Email</h3>"
                + $"<p>請點擊以下連結完成 Email 驗證（{EmailVerifyTokenLifetime.TotalHours:0} 小時內有效）：</p>"
                + $"<a href=\"{System.Net.WebUtility.HtmlEncode(verifyUrl)}\">驗證 Email</a>"
                + "<p>如果這不是您註冊的帳號，請忽略這封信；之後您可以直接用此 Email 的 Google 帳號登入取回帳號。</p>";
            await _emailService.SendEmailAsync(user.Email, "驗證您的 Email", emailContent);

            return SendVerificationResult.Sent;
        }

        // 5. 驗證 Email（驗證信連結）
        public async Task<bool> VerifyEmailAsync(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)) return false;

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;
            if (user.EmailVerified) return true; // 重複點連結

            if (string.IsNullOrEmpty(user.EmailVerifyToken)
                || user.EmailVerifyTokenExpiry == null
                || user.EmailVerifyTokenExpiry < DateTime.Now
                || !TokenEquals(user.EmailVerifyToken, token))
                return false;

            user.EmailVerified = true;
            user.EmailVerifyToken = null;
            user.EmailVerifyTokenExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }

        // 6. Google 登入：找不到就建立 Google 帳號。
        //    同一個 Email 已經有「密碼註冊、尚未驗證 Email」的帳號時，Google 已證明 Email 屬於本人 → 接管：
        //    清除密碼（之後只能用 Google 登入）、換新登入戳記（登出所有裝置）、標記為已驗證。
        //    避免先用別人的 Email 註冊，等對方用 Google 登入後共用同一個帳號
        public async Task<(User User, bool TakenOver)> GetOrCreateGoogleUserAsync(string email, string? name)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return (await _userService.CreateGoogleUserAsync(email, name), false);

            bool takenOver = false;
            if (!user.EmailVerified)
            {
                if (user.Password != UserService.GoogleAccountPassword)
                {
                    user.Password = UserService.GoogleAccountPassword;
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;
                    user.SecurityStamp = NewSecurityStamp();
                    takenOver = true;
                }
                user.EmailVerified = true;
                user.EmailVerifyToken = null;
                user.EmailVerifyTokenExpiry = null;
                await _context.SaveChangesAsync();
                MemberCookieValidator.Evict(_cache, user.UserId);
            }

            return (user, takenOver);
        }
    }
           
}
