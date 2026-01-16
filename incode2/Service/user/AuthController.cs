using incode2.Data;
using incode2.DTO.user;
using incode2.Models; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Service.user
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto register)
        {
            // 1. 檢查 Email 是否已存在
            var existingUser = await _userManager.FindByEmailAsync(register.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "此 Email 已被註冊" });
            }

            // 2. 建立使用者物件
            // 注意：Identity 要求 UserName 不能為空，通常我們將 Email 填入 UserName
            var user = new ApplicationUser
            {
                UserName = register.Email,
                Email = register.Email,
                // 如果 ApplicationUser 有自定義欄位，在這裡賦值 Name = register.Username
                
            };

            // 3. 建立帳號
            // CreateAsync 會自動進行密碼雜湊 (Hashing) 處理
            var result = await _userManager.CreateAsync(user, register.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "註冊成功，請登入" });
            }
            else
            {
                // 回傳具體的錯誤原因 (例如：密碼需包含大寫字母)
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = "註冊失敗", details = errorMsg });
            }
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            // 1. 先確認帳號存在
            var user = await _userManager.FindByEmailAsync(login.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 2. 使用 SignInManager 登入
            // 參數說明: (user, password, isPersistent, lockoutOnFailure)
            // 這行程式碼會自動處理 Scheme 和 Cookie，避免 500 錯誤
            var result = await _signInManager.PasswordSignInAsync(user, login.Password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = "登入成功",
                    username = user.UserName,
                    userId = user.Id
                });
            }

            if (result.IsLockedOut)
            {
                return Unauthorized(new { message = "帳號已被鎖定，請稍後再試" });
            }

            return Unauthorized(new { message = "帳號或密碼錯誤" });
        }

        // POST: api/Auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            // 清除 Session (如果有用的話)
            HttpContext.Session.Clear();

            return Ok(new { message = "已登出" });
        }

        // GET: api/Auth/check
        [HttpGet("check")]
        public IActionResult CheckLogin()
        {
            // User.Identity.IsAuthenticated 會自動讀取 Cookie 驗證狀態
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Ok(new { isLogin = true, name = User.Identity.Name });
            }
            return Unauthorized(new { isLogin = false });
        }
    }
}
