using Azure.Core;
using incode2.Dtos.Users;
using incode2.Enums;
using incode2.Extensions;
using incode2.Services.Users;
using incode2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.ApiControllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthApiController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        ///AuthApi/Register
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
                    var loginResult = await _authService.ValidateUserAsync(dto.Email, dto.Password);
                    
                    if (!loginResult.IsSuccess || loginResult.Member == null)
                    {
                        return BadRequest(new { message = "註冊成功但自動登入失敗" });
                    }

                    var newUser = loginResult.Member;
                    await SignInProcessAsync(newUser, false); // false 代表註冊預設不記住我
                    string roleName = newUser.RoleId == (int)RoleType.Admin ? "Admin" : "Member";

                    return Ok(new
                    {
                        message = "註冊成功並已登入",
                        user = new
                        {
                            userid = newUser.UserId,
                            username = newUser.Username,
                            email = newUser.Email,
                            role = roleName,
                            avatarUrl = newUser.ImageUrl
                        }
                    });
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
                    error = ex.Message
                });
            }
        }

        private async Task SignInProcessAsync(User user, bool rememberMe)
        {
            string roleName = user.RoleId == (int)RoleType.Admin ? "Admin" : "Member";

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username ?? user.Email),
        new Claim(ClaimTypes.Role, roleName)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginData)
        {
            // 1. 呼叫 Service 驗證帳密
            var result = await _authService.ValidateUserAsync(loginData.Email, loginData.Password);

            if (!result.IsSuccess)
            {                
                return BadRequest(new { message = result.Message });
            }

            // 抓取 IP
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress)) ipAddress = "127.0.0.1"; // 本機開發 (Localhost)，IP 可能是 "::1"
            // (進階) 如果你的網站架在 Cloudflare 或 Load Balancer
            //if (Request.Headers.ContainsKey("X-Forwarded-For"))
            //{
            //    ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            //}

            await _userService.AddLoginLogAsync(result.Member.UserId, ipAddress);

            var user = result.Member;            
            if (user == null) return BadRequest(new { message = "登入資料異常" });
            await SignInProcessAsync(user, loginData.RememberMe); // 呼叫共用方法簽發 Cookie
            string roleName = user.RoleId == (int)RoleType.Admin ? "Admin" : "Member";
            
            return Ok(new
            {
                message = result.Message,
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
            if (Request.Cookies[".AspNetCore.Cookies"] != null)
            {
                Response.Cookies.Delete(".AspNetCore.Cookies");
            }
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