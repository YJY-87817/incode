using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace incode.MVC.Controllers.Users
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous] // 允許未登入者訪問
        public IActionResult Login()
        {
            // 如果已經登入且是管理員，直接踢回首頁，不要讓他在登入頁鬼打牆
            if (User.Identity!.IsAuthenticated && User.Identities.Any(i => i.AuthenticationType == "AdminAuth"))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. 驗證帳密
            var result = await _authService.ValidateUserAsync(model.Email, model.Password);

            // 2. 基礎驗證失敗 (帳密錯或無此人)
            if (!result.IsSuccess || result.Member == null)
            {
                ModelState.AddModelError(string.Empty, result.IsLockedOut ? result.Message : "帳號或密碼錯誤");
                return View(model);
            }

            // 3. 安全性檢查：雖然帳密對，但身分必須是 Admin
            if (result.Member.RoleId != (int)RoleType.Admin)
            {
                ModelState.AddModelError(string.Empty, "權限不足，您不是管理員");
                return View(model);
            }

            // 4. 建立 Claims (身分證內容)
            var claims = new List<Claim>
            {
                // 顯示用的名字
                new Claim(ClaimTypes.Name, result.Member.Username ?? result.Member.Email),
                
                // 角色權限
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, result.Member.UserId.ToString()),
                new Claim("UserId", result.Member.UserId.ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(claims, "AdminAuth");
            var authProperties = new AuthenticationProperties
            {
                // 根據使用者是否勾選「記住我」(這裡先預設為 true，或者您可以從 model.RememberMe 傳入)
                IsPersistent = true,
            };

            // 5. 簽發 "AdminAuth" Cookie
            await HttpContext.SignInAsync(
                "AdminAuth",
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // 只清除後台 Cookie
            await HttpContext.SignOutAsync("AdminAuth");
            return RedirectToAction("Login");
        }
    }
}