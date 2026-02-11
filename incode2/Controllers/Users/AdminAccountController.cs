using incode2.Enums;
using incode2.Services.Users;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization; // 記得引用
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.Controllers
{
    [Route("Admin/Account/[action]")]
    public class AdminAccountController : Controller
    {
        private readonly IAuthService _authService;

        public AdminAccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous] // 允許未登入者訪問
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous] // 允許未登入者訪問
        [ValidateAntiForgeryToken] // 安全性防護
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. 驗證帳密
            var result = await _authService.ValidateUserAsync(model.Email, model.Password);

            // *** 修正點：必須先檢查 IsSuccess ***
            if (!result.IsSuccess || result.Member == null)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤");
                return View(model);
            }

            // 2. 檢查權限：只有 Admin 才能登入後台
            if (result.Member.RoleId != (int)RoleType.Admin)
            {
                ModelState.AddModelError("", "權限不足，您不是管理員");
                return View(model);
            }

            // 3. 發放 "AdminAuth" 通行證
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, result.Member.Username ?? result.Member.Email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, result.Member.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "AdminAuth"); // <--- 關鍵

            await HttpContext.SignInAsync("AdminAuth", new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = false // 後台不建議記住登入
                });

            // 4. 導向後台首頁 (請確保 DashboardController 存在且有 [Area("Admin")])
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            // 只登出後台
            await HttpContext.SignOutAsync("AdminAuth");
            return RedirectToAction("Login");
        }
    }
}