using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace incode.MVC.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {        
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly string _frontendUrl;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(IAuthService authService, IUserService userService, IConfiguration configuration, ILogger<AuthApiController> logger)
        {
            _authService = authService;
            _userService = userService;
            _frontendUrl = configuration.GetFrontendUrl();
            _logger = logger;
        }

        ///AuthApi/Register
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
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

                    // 寄 Email 驗證信（不擋登入；寄信失敗不影響註冊，會員中心可以重寄）
                    await TrySendVerificationEmailAsync(newUser.UserId);
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
                _logger.LogError(ex, "註冊失敗");
                return StatusCode(500, new { message = "伺服器內部錯誤，請稍後再試" });
            }
        }

        private async Task SignInProcessAsync(User user, bool rememberMe)
        {
            string roleName = user.RoleId == (int)RoleType.Admin ? "Admin" : "Member";

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username ?? user.Email),
        new Claim(ClaimTypes.Role, roleName),
        // 登入戳記：重設 / 修改密碼後資料庫換新，這個 Cookie 就會失效（MemberCookieValidator）
        new Claim(AuthSchemes.SecurityStampClaim, user.SecurityStamp ?? "")
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
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginData)
        {
            // 1. 呼叫 Service 驗證帳密
            var result = await _authService.ValidateUserAsync(loginData.Email, loginData.Password);

            // 登入安全事件只記來源 IP，不記 Email / 密碼
            if (result.IsLockedOut)
            {
                _logger.LogWarning("登入被暫停（失敗次數過多） - IP: {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = result.Message });
            }

            if (!result.IsSuccess)
            {
                _logger.LogInformation("登入失敗 - IP: {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
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
            // Google 回呼寫在暫存 Cookie（AuthSchemes.External），讀完就清除
            var result = await HttpContext.AuthenticateAsync(AuthSchemes.External);
            await HttpContext.SignOutAsync(AuthSchemes.External);

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

            // 找不到就建立 Google 帳號；同 Email 的未驗證密碼帳號會被接管（清除密碼、登出其他裝置）
            var (user, takenOver) = await _authService.GetOrCreateGoogleUserAsync(email, name);
            if (takenOver)
                _logger.LogWarning("Google 登入接管未驗證 Email 的密碼帳號 - UserId: {UserId}", user.UserId);

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

        // 修改密碼（會員中心）：需要目前的密碼；成功後其他裝置登出，目前的裝置重新簽發 Cookie 保持登入
        [HttpPost("change-password")]
        [Authorize(AuthenticationSchemes = AuthSchemes.Member)] // 會員本人（不是後台 Cookie）
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var (result, user) = await _authService.ChangePasswordAsync(User.GetId(), request.CurrentPassword, request.NewPassword);
            switch (result)
            {
                case ChangePasswordResult.Success:
                    // 保留原本的「記住我」設定
                    var current = await HttpContext.AuthenticateAsync(AuthSchemes.Member);
                    await SignInProcessAsync(user!, current.Properties?.IsPersistent ?? false);
                    return Ok(new { message = "密碼已更新，其他裝置的登入已登出" });
                case ChangePasswordResult.GoogleAccount:
                    return BadRequest(new { message = "此帳號使用 Google 登入，沒有密碼可以修改" });
                case ChangePasswordResult.LockedOut:
                    return StatusCode(StatusCodes.Status429TooManyRequests,
                        new { message = $"密碼錯誤次數過多，請 {LoginAttemptLimiter.Window.TotalMinutes:0} 分鐘後再試" });
                case ChangePasswordResult.WrongPassword:
                    return BadRequest(new { message = "目前的密碼不正確" });
                case ChangePasswordResult.InvalidNewPassword:
                    return BadRequest(new { message = "新密碼需為 6～100 碼" });
                default:
                    return NotFound(new { message = "找不到使用者" });
            }
        }

        // 驗證 Email（驗證信連結 → 前端 /verify-email 頁面呼叫）
        [HttpPost("verify-email")]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (await _authService.VerifyEmailAsync(request.Email, request.Token))
                return Ok(new { message = "Email 驗證成功" });
            return BadRequest(new { message = "驗證連結無效或已過期，請從會員中心重寄驗證信" });
        }

        // 重寄 Email 驗證信（會員中心）
        [HttpPost("resend-verification")]
        [Authorize(AuthenticationSchemes = AuthSchemes.Member)] // 會員本人（不是後台 Cookie）
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        public async Task<IActionResult> ResendVerification()
        {
            try
            {
                var result = await _authService.SendVerificationEmailAsync(User.GetId(), VerifyEmailUrl);
                return result switch
                {
                    SendVerificationResult.Sent => Ok(new { message = "驗證信已寄出，請到信箱收信" }),
                    SendVerificationResult.AlreadyVerified => Ok(new { message = "Email 已經驗證過了" }),
                    _ => NotFound(new { message = "找不到使用者" }),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "寄送 Email 驗證信失敗");
                return StatusCode(500, new { message = "驗證信寄送失敗，請稍後再試" });
            }
        }

        private string VerifyEmailUrl => $"{_frontendUrl}/verify-email";

        private async Task TrySendVerificationEmailAsync(int userId)
        {
            try
            {
                await _authService.SendVerificationEmailAsync(userId, VerifyEmailUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "註冊後寄送 Email 驗證信失敗 - UserId: {UserId}", userId);
            }
        }

        // 前端 Vue 請 POST 到: /api/Auth/forgot-password
        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            // 這裡的網址要設定為 "前端 Vue 的網址"！
            // 假設你的 Vue 跑在 port 5173
            string frontendUrl = $"{_frontendUrl}/reset-password";

            // 呼叫 Service (Service 內寄信時，會把 token 拼在 frontendUrl 後面)
            try
            {
                await _authService.ForgotPasswordAsync(request.Email, frontendUrl);
            }
            catch (Exception ex)
            {
                // 寄信失敗（SMTP 設定錯誤、網路）：原本例外直接拋出，開發環境會回傳完整的例外頁面
                _logger.LogError(ex, "寄送重設密碼信失敗");
                return StatusCode(500, new { message = "重設密碼信寄送失敗，請稍後再試" });
            }

            // API 永遠只回傳狀態，不回傳畫面
            return Ok(new { message = "若 Email 存在，信件已發送。" });
        }

        // API 2: 重設密碼
        // 前端 Vue 請 POST 到: /api/Auth/reset-password
        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
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