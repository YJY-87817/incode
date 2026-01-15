using incode2.DTO.user;
using incode2.Models;
using Microsoft.AspNetCore.Identity;
using incode2.Data;

namespace incode2.Service.user
{
       public class AuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly incodedatabaseContext _context;

        public AuthService(
             SignInManager<ApplicationUser> signInManager,
             UserManager<ApplicationUser> userManager,
             incodedatabaseContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        public void Register(RegisterDto registerDto)
        {
            var user = new User
            {
                Email = registerDto.Email,
                Password = registerDto.Password,
                // 根據 RegisterDto 和 User 的定義補齊其他必要欄位
            };
            _context.User.Add(user);
            _context.SaveChanges();
        }
        public User? ValidateUser(string email, string password)
        {
            // 這裡應該要有密碼雜湊的驗證，這裡簡化處理
            var user = _context.User.FirstOrDefault(u => u.Email == email && u.Password == password);
            return user;
        }

        public async Task<ServiceResponse<string>> LoginAsync(string email, string password)
        {
            // 1. 使用 UserManager 找使用者 (不要用 _context.Users.Where...)
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new ServiceResponse<string> { Success = false, Message = "找不到此帳號" };
            }

            // 2. 使用 SignInManager 進行登入驗證
            // PasswordSignInAsync 會自動幫你驗證密碼雜湊，並且「寫入 Cookie」
            // 這是解決 "No sign-in scheme" 的關鍵！
            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                isPersistent: true, // 是否記住我
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return new ServiceResponse<string> { Success = true, Data = "登入成功" };
            }

            if (result.IsLockedOut)
            {
                return new ServiceResponse<string> { Success = false, Message = "帳號已被鎖定" };
            }

            return new ServiceResponse<string> { Success = false, Message = "密碼錯誤" };
        }

        public async Task<ServiceResponse<string>> RegisterAsync(RegisterDto registerDto)
        {
            //  檢查是否已存在
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new ServiceResponse<string> { Success = false, Message = "此 Email 已被註冊" };
            }

            //  建立 Identity 使用者物件
            string namePart = registerDto.Email.Contains("@")
            ? registerDto.Email.Split('@')[0] : registerDto.Email;
            // string uniqueName = $"{namePart}_{new Random().Next(1000, 9999)}"; 避免UserName 重複
            var newUser = new ApplicationUser
            {
                UserName = namePart, 
                Email = registerDto.Email,
            };

            // 使用 CreateAsync 建立使用者並自動 Hash 密碼
            var result = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (result.Succeeded)
            {
                // 預設是一般會員
                await _userManager.AddToRoleAsync(newUser, "Member");
                return new ServiceResponse<string> { Success = true, Data = "註冊成功" };
            }

            // 4. 如果失敗 (例如密碼太簡單)，回傳錯誤原因
            var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ServiceResponse<string> { Success = false, Message = errorMsg };
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }
    }
}