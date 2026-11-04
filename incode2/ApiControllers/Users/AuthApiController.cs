using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {        
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly string _frontendUrl;

        public AuthApiController(IAuthService authService, IUserService userService, IConfiguration configuration)
        {
            _authService = authService;
            _userService = userService;
            _frontendUrl = configuration.GetFrontendUrl();
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
                var result = await _authService.RegisterAsync(dto.Email, dto.Password, dto.Username, dto.ImageUrl);

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

            var claimsIdentity = new ClaimsIdentity(claims, "MemberAuth");

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                "MemberAuth",
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

        // 1. 前端點擊「Google 登入」後，導向這裡
        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                // Google 登入成功後，後端會自動回調這個網址 (GoogleResponse)
                RedirectUri = Url.Action(nameof(GoogleResponse))
            };
            // 發起挑戰，導向 Google 登入頁面
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // 2. Google 登入成功後的回調 (由後端內部處理)
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            // 取得 Google 回傳的使用者資訊
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest("Google 驗證失敗");

            // 抓取 Google 給的資料
            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            // Google 的大頭貼 (通常是 picture 或 urn:google:picture)
            // var picture = claims?.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("無法取得 Google Email 資訊");

            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
            {
                user = await _userService.CreateGoogleUserAsync(email, name);
            }

            // --- 執行登入 (寫入 Cookie) ---
            await SignInProcessAsync(user, true); // Google 登入預設記住我

            // --- 最後：重導回前端 Vue --
            return Redirect($"{_frontendUrl}/userindex");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MemberAuth");
            await HttpContext.SignOutAsync("AdminAuth");
           
            if (Request.Cookies["Incode.Member"] != null)
            {
                Response.Cookies.Delete("Incode.Member", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,                // 必須與 Program.cs 一致
                    SameSite = SameSiteMode.None, // 必須與 Program.cs 一致
                    Path = "/"                    // 必須與 Program.cs 一致
                });
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

        // 前端 Vue 請 POST 到: /api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // 這裡的網址要設定為 "前端 Vue 的網址"！
            // 假設你的 Vue 跑在 port 5173
            string frontendUrl = $"{_frontendUrl}/reset-password";

            // 呼叫 Service (Service 內寄信時，會把 token 拼在 frontendUrl 後面)
            await _authService.ForgotPasswordAsync(request.Email, frontendUrl);

            // API 永遠只回傳狀態，不回傳畫面
            return Ok(new { message = "若 Email 存在，信件已發送。" });
        }

        // API 2: 重設密碼
        // 前端 Vue 請 POST 到: /api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            bool result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (result)
            {
                return Ok(new { message = "密碼重設成功" });
            }
            else
            {
                return BadRequest(new { message = "連結無效或已過期" });
            }
        }

    }
}