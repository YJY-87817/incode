using incode2.Models;

namespace incode2.Services.Users
{
    public class AuthService : IAuthService
    {       
        private readonly IPasswordService _passwordService;
        private readonly IUserService _userService;

        public AuthService(IPasswordService passwordService, IUserService userService)
        {
            _passwordService = passwordService;
            _userService = userService;
        }
        public async Task<string> RegisterAsync(string email, string password, string? preferredUsername)
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

            return new LoginResult
            {
                IsSuccess = true,
                Message = "登入成功",
                Member = user
            };
        }
    }        
}
