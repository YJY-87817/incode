using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using incode2.Service;
using incode2.DTO;

namespace incode2.ApiController
{
    [Route("api/Auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        // 注入 User Service
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                // 存入權限等級，後續 Filter 會用到
                new Claim("RoleId", user.RoleId.ToString())
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
            return Ok(new { message = "登入成功", username = user.Username });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 登出
            await HttpContext.SignOutAsync("MyCookieAuth");

            // 如果有使用 Session 存暫存資料，順便清空
            HttpContext.Session.Clear();

            return Ok(new { message = "已登出" });
        }

        // 增加一個 API 讓 Vue 確認登入狀態
        [HttpGet("check")]
        public IActionResult CheckLogin()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Ok(new { isLogin = true, name = User.Identity.Name });
            }
            return Unauthorized(new { isLogin = false });
        }
    }
}
