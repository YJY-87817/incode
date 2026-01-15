using incode2.DTO.user;
using incode2.Service.user;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.Service.user
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public AuthController(AuthService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto register)
        {
            if (await _authService.UserExistsAsync(register.Email))
            {
                return BadRequest(new { message = "此 Email 已被註冊" });
            }

            if (string.IsNullOrEmpty(register.Username))
            {
                register.Username = register.Email.Split('@')[0];
                // 只用 @ 前面的字
            }

            await _authService.RegisterAsync(register);
            return Ok(new { message = "註冊成功，請登入" });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            // [ApiController] 會自動驗證 login 格式，這裡不需要寫 ModelState.IsValid

            var user = _userService.ValidateUser(login.Email, login.Password);
            if (user == null)
            {
                // API 標準回傳：401 Unauthorized
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 1. 建立身分資訊 (Claims)
            // 保護可能的 null 值，避免傳入 Claim 時丟出 CS8604 / NullReferenceException
            var usernameClaimValue = user.Username ?? string.Empty;
            var roleClaimValue = (user.RoleId?.ToString()) ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, usernameClaimValue),
                new Claim(ClaimTypes.Role, roleClaimValue) // 存入權限等級，後續 Filter 會用到
            };

            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 2. 簽發 Cookie (登入)
            await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            });

            // 回傳 JSON 給 Vue
            return Ok(new { message = "登入成功", username = usernameClaimValue });
        }

        // POST: api/Auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 登出
            await HttpContext.SignOutAsync("MyCookieAuth");

            // 如果有使用 Session 存暫存資料，順便清空
            HttpContext.Session.Clear();

            return Ok(new { message = "已登出" });
        }

        // GET: api/Auth/check
        [HttpGet("check")]// 增加一個 API 讓 Vue 確認登入狀態
        public IActionResult CheckLogin()
        {
            // 使用 null-conditional 確保 User 或 Identity 為 null 時不會發生 NullReferenceException
            if (User?.Identity?.IsAuthenticated == true)
            {
                var name = User.Identity?.Name ?? string.Empty;
                return Ok(new { isLogin = true, name });
            }
            return Unauthorized(new { isLogin = false });
        }
    }
}
