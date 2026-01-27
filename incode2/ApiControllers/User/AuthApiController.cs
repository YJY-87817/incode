using Azure.Core;
using incode2.Dtos.Users;
using incode2.Enums;
using incode2.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using incode2.Extensions;

namespace incode2.ApiControllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthApiController(IAuthService authService)
        {
            _authService = authService;
        }

        //
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); 
            }

            try
            {
                var result = await _authService.RegisterAsync(dto.Email, dto.Password, dto.Username);

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
                return StatusCode(500, new
                {
                    message = "伺服器內部錯誤",
                    error = ex.Message,
                    inner = ex.InnerException?.Message 
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
            string roleName = user.RoleId == (int)RoleType.Admin ? "Admin" : "Member";

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
                user = new
                {
                    id = user.UserId,          
                    username = user.Username,
                    email = user.Email,       
                    role = roleName,
                    avatarUrl = user.ImageUrl 
                }
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
                int currentUserId = User.GetId();

                return Ok(new
                {
                    isLoggedIn = true,
                    userId = currentUserId,
                    username = User.Identity.Name,
                    role = User.FindFirst(ClaimTypes.Role)?.Value
                });
            }

            return Ok(new { isLoggedIn = false });
        }
    }
}