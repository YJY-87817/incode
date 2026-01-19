using incode2.Models;

namespace incode2.Services.user
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
        public async Task<string> RegisterAsync(string email, string password)
        {
            // 1. 檢查 Email (呼叫 UserService)
            if (await _userService.IsEmailExistAsync(email))
            {
                return "Email 已被註冊";
            }

            // 2. 加密密碼 (呼叫 PasswordService)
            string passwordHash = _passwordService.Hash(password);
            // 自動產生 Username (Email @ 前贅詞 + 2位亂碼)
            string generatedUsername = GenerateUsernameFromEmail(email);

            // 3. 準備資料
            var newUser = new User
            {
                Email = email,
                Username = generatedUsername,
                Password = passwordHash,
                CreatedAt = DateTime.Now,
                RoleId = 2 // 預設 Member
            };

            // 4. 存檔 (呼叫 UserService)
            await _userService.CreateUserAsync(newUser);

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
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }  

        //  登入驗證
        public async Task<User?> ValidateUserAsync(string email, string password)
            {
            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                return null; // 找不到人
            }

            // 2. 驗證密碼
            bool isPasswordCorrect = _passwordService.Verify(password, user.Password);

            if (isPasswordCorrect)
            {
                return user; // 密碼對了，把整個人(包含 UserId)回傳出去
            }

            return null; // 密碼錯了
        }
    }        
}
