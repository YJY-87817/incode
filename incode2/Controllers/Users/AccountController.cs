using incode2.Enums;
using incode2.Services.Users;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.Controllers.Users
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        // 注入 AuthService
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Register (顯示畫面)
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register (接收表單)
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止 CSRF 攻擊
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // 1. 檢查資料格式 (前端驗證過了，後端要再驗一次)
            if (!ModelState.IsValid)
            {
                return View(model); // 有錯就回傳原畫面，讓使用者修正
            }

            // 2. 呼叫 Service 進行註冊 (使用您剛剛定義的 ServiceResult)
            var result = await _authService.RegisterAsync(model.Email, model.Password, model.Username);

            if (result != "Success")
            {
                // 如果失敗 (例如 Email 重複)，將錯誤訊息加到 ModelState
                // string.Empty 代表這是全域錯誤，會顯示在 asp-validation-summary
                ModelState.AddModelError(string.Empty, result);
                return View(model);
            }

            // 3. 註冊成功，導向登入頁面 (或是直接幫他登入)
            TempData["SuccessMessage"] = "註冊成功！請使用新帳號登入。";
            return RedirectToAction("Login");
        }

        //GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // 如果已經登入，直接踢回首頁
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(); // 這會去找 Views/Account/Login.cshtml
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. 驗證帳密
            var result = await _authService.ValidateUserAsync(model.Email, model.Password);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            var user = result.Member;
            string roleName = (user.RoleId == (int)RoleType.Admin) ? "Admin" : "Member";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                // 可以根據需求設定是否記住登入
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            };

            // 3. 登入 (寫入 Cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 4. 登入成功，導向首頁
            return RedirectToAction("Index", "Home");
        }

        // 登出 (POST)
        // 對應 _LoginPartial 中的表單 submit
        [HttpPost]
        [ValidateAntiForgeryToken] // MVC 表單建議加上這行防護
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 登出後導向首頁
            return RedirectToAction("Index", "Home");
        }
    }
}

