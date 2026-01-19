using Azure.Core;
using incode2.DTO.user;
using incode2.Enums;
using incode2.Service.user;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.ApiController.user
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. 後端驗證：這裡會自動檢查 ConfirmPassword 是否等於 Password
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // 如果不一致，這裡直接退回 400 錯誤
            }

            try
            {
                var result = await _authService.RegisterAsync(dto.Email, dto.Password);

                if (result == "Success")
                {
                    return Ok(new { message = "註冊成功" });
                }
                else
                {
                    return BadRequest(new { message = result });
                }
            }
            catch (Exception ex)
            {
                // 捕捉 500 錯誤並回傳給前端看 (開發階段用)
                return StatusCode(500, new
                {
                    message = "伺服器內部錯誤",
                    error = ex.Message,
                    inner = ex.InnerException?.Message // 這裡通常會顯示 RoleId 錯誤的原因
                });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginData)
        {
            // 1. 呼叫 Service 驗證帳密
            var user = await _authService.ValidateUserAsync(loginData.Email, loginData.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 2. 準備 Cookie 的內容 (Claims)
            // 這裡把 RoleId 轉成易讀的字串，方便前端判斷
            string roleName = (user.RoleId == (int)RoleType.Admin) ? "Admin" : "Member";

            var claims = new List<Claim>
            {
                // 放入 UserId (這是最重要的，之後都靠這個認人)
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                // 放入顯示名稱(有使用者名稱就用名稱，沒有的話就用 Email 代替
                new Claim(ClaimTypes.Name, user.Username ?? user.Email),
                // 放入權限角色
                new Claim(ClaimTypes.Role, roleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = loginData.RememberMe, // 記住我 (關閉瀏覽器後 Cookie 還在)
                // 設定過期時間 (例如：如果有勾記住我，就存 7 天；沒勾就存 30 分鐘，或是不設定由瀏覽器決定)
                ExpiresUtc = loginData.RememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30)
            };

            // 3. 簽發 Cookie (瀏覽器會收到 Set-Cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(new
            {
                message = "登入成功",
                user = new { user.Username, Role = roleName }
            });
        }
               
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 清除 Cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "已登出" });
        }

        [HttpGet("check")] //  檢查狀態 (Check Status) - 讓前端知道現在是否登入
        public IActionResult CheckStatus()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Ok(new
                {
                    isLoggedIn = true,
                    username = User.Identity.Name,
                    role = User.FindFirst(ClaimTypes.Role)?.Value
                });
            }

            return Ok(new { isLoggedIn = false });
        }
    }
}