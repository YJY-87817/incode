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

        public AuthService(incodedatabaseContext context, IPasswordService passwordService, IEmailService emailService, IUserService userService, IMissionService missionService)
        {
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
            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
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
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = "帳號或密碼錯誤"
                };
            }

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

            // A. 產生 Token (使用 Guid 產生亂碼)
            string token = Guid.NewGuid().ToString();

            // B. 存入資料庫 (設定 30 分鐘後過期)
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(30);

            _context.Update(user);
            await _context.SaveChangesAsync();

            // C. 組合重設連結 (例如: https://localhost/Auth/ResetPassword?email=...&token=...)
            string resetUrl = $"{resetLinkBaseUrl}?email={email}&token={token}";

            // D. 寄信
            string emailContent = $"<h3>重設密碼通知</h3><p>請點擊以下連結重設您的密碼：</p><a href='{resetUrl}'>重設密碼</a>";
            await _emailService.SendEmailAsync(email, "重設密碼通知", emailContent);

            return true;
        }

        // 2. 執行重設密碼
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

            // 驗證: 找不到人 OR Token 不對 OR Token 過期了
            if (user == null || user.ResetToken != token || user.ResetTokenExpiry < DateTime.Now)
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

            return true;
        }
    }
           
}
