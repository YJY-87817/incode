using incode2.Enums;
using incode2.Services.Users;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.Controllers.Users
{
    [Route("Admin/Account/[action]")]
    public class AdminAccountController : Controller
    {
        private readonly IAuthService _authService;
        public AdminAccountController(IAuthService authService) { _authService = authService; }
        [HttpGet]
        public IActionResult Login() => View();

        // 2. 後台登入動作
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // ... 驗證帳號密碼 ...
            var result = await _authService.ValidateUserAsync(model.Email, model.Password);

            // 安全檢查：只有 Admin 才能登入後台
            if (result.Member.RoleId != (int)RoleType.Admin)
            {
                ModelState.AddModelError("", "權限不足");
                return View(model);
            }

            // *** 關鍵：發放 "AdminAuth" 通行證 ***
            var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, result.Member.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };
            var identity = new ClaimsIdentity(claims, "AdminAuth"); // <--- 重點

            await HttpContext.SignInAsync("AdminAuth", new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false }); // 後台通常不記住登入

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // 3. 後台登出
        public async Task<IActionResult> Logout()
        {
            // *** 關鍵：只撕毀 "AdminAuth" 通行證 ***
            await HttpContext.SignOutAsync("AdminAuth");
            return RedirectToAction("Login");
        }
    }
}
