using incode2.Dtos.Users;
using incode2.Enums;
using incode2.Models;
using incode2.Partials;
using incode2.Services.Users;
using incode2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Controllers.Users
{
    [Authorize(AuthenticationSchemes = "AdminAuth", Roles = "Admin")]
    public class MemberController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public MemberController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }
        // GET: /Member/Index
        public async Task<IActionResult> Index()
        {
            // 假設 UserService 有 GetAllUsersAsync
            var users = await _userService.GetAllUsersAsync();

            // 簡單轉成 VM 顯示
            var vms = users.Select(u => new MemberIndexViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                Birthday = u.Birthday,
                Address = u.Address,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive ?? true,
                RoleName = u.RoleId == (int)RoleType.Admin ? "管理員" : "一般會員"
            }).ToList();

            return View(vms);
        }

        // GET: /Member/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Member/Create
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create(MemberCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // 呼叫 AuthService 新增
            // 這裡假設 RegisterAsync 回傳字串錯誤訊息，如果是 "Success" 代表成功
            string result = await _authService.RegisterAsync(vm.Email, vm.Password, vm.Username);

            if (result == "Success")
            {
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("Email", result); // 顯示錯誤 (如 Email 重複)
                return View(vm);
            }
        }

        // GET: /Member/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            // 撈取前 10 筆 Log
            var logs = await _userService.GetLoginLogsByUserIdAsync(id, 10);

            var vm = new MemberEditViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive ?? true,

                // 轉換 Log
                RecentLoginLogs = logs.Select(l => new LoginLogDto
                {
                    LoginTime = l.LoginTime,
                    IpAddress = l.IpAddress,
                    // 簡易判斷異常 (例如: IP 不同或是 Service 算好的欄位)
                    IsSuspicious = false // 這裡需依你的邏輯判斷
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Member/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MemberEditViewModel vm)
        {
            // EditViewModel 的 Password 不是必填，所以 ModelState 驗證通常會過
            if (!ModelState.IsValid) return View(vm);

            var user = await _userService.GetUserByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            // 更新基本資料
            user.Username = vm.Username;
            user.Email = vm.Email;

            // --- 關鍵：更新帳號狀態 (停權/啟用) ---
            user.IsActive = vm.IsActive;

            // 只有當管理員有填寫密碼欄位時，才幫使用者重設密碼
            if (!string.IsNullOrEmpty(vm.Password))
            {
                // 注意：這裡需要 Hash 密碼，建議抽出 Helper 或 Service
                // user.Password = _passwordService.Hash(vm.Password);
            }

            await _userService.UpdateUserAsync(user);

            return RedirectToAction("Index");
        }
    }
}
