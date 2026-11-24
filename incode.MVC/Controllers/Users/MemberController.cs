using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.Controllers.Users
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
            var users = await _userService.GetAllUsersAsync();
            var vms = new List<MemberIndexViewModel>();

            // 所有會員最近 10 筆登入紀錄一次查出（原本每位會員各查一次）
            var recentLogs = await _userService.GetRecentLoginLogsForAllUsersAsync(10);

            // 針對每個使用者，檢查是否有異常登入
            foreach (var u in users)
            {
                var logs = recentLogs.GetValueOrDefault(u.UserId) ?? new List<LoginLogDto>();

                // 判斷主要 IP
                var mainIp = logs.GroupBy(l => l.IpAddress)
                                 .OrderByDescending(g => g.Count())
                                 .Select(g => g.Key)
                                 .FirstOrDefault();

                // 如果有任何一筆 Log 的 IP 不是主要 IP，就標記為有異常
                bool hasSuspicious = logs.Any(l => !string.IsNullOrEmpty(mainIp) && l.IpAddress != mainIp);

                vms.Add(new MemberIndexViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Phone = u.Phone,
                    Birthday = u.Birthday,
                    Address = u.Address,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive ?? true,
                    RoleName = u.RoleId == (int)RoleType.Admin ? "管理員" : "一般會員",
                    HasSuspiciousLogin = hasSuspicious 
                });
            }

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
            // 找出這 10 筆紀錄中，出現最多次的 IP 作為「主要/常用 IP」
            var mainIp = logs.GroupBy(l => l.IpAddress)
                     .OrderByDescending(g => g.Count())
                     .Select(g => g.Key)
                     .FirstOrDefault();

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
                    // 判斷：如果這次的 IP 不是最常用的 IP，就標記為異常 (true)
                    IsSuspicious = !string.IsNullOrEmpty(mainIp) && l.IpAddress != mainIp
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

            // Email / 暱稱有唯一索引：與其他會員重複時顯示錯誤（原本存檔時 500）
            if (await _userService.IsEmailTakenByOtherAsync(vm.Email, vm.UserId))
                ModelState.AddModelError(nameof(vm.Email), "此 Email 已被其他會員使用");
            if (string.IsNullOrWhiteSpace(vm.Username))
                ModelState.AddModelError(nameof(vm.Username), "請輸入暱稱");
            else if (vm.Username != user.Username && !UsernameRules.IsValid(vm.Username))
                ModelState.AddModelError(nameof(vm.Username), UsernameRules.ErrorMessage); // 沒改暱稱就不檢查（舊暱稱可能不符合規則）
            else if (await _userService.IsUsernameTakenByOtherAsync(vm.Username, vm.UserId))
                ModelState.AddModelError(nameof(vm.Username), "此暱稱已被其他會員使用");
            // 不能在這裡停用自己的帳號（前台 API 也擋；停用後會立刻被登出後台）
            if (vm.UserId == User.GetId() && !vm.IsActive)
                ModelState.AddModelError(nameof(vm.IsActive), "不能停用自己的帳號");
            bool resetPassword = !string.IsNullOrEmpty(vm.Password);
            if (resetPassword && user.Password == UserService.GoogleAccountPassword)
                ModelState.AddModelError(nameof(vm.Password), "此會員使用 Google 登入，沒有密碼可以重設");
            // 重設密碼會換新登入戳記：在這裡改自己的密碼，目前的後台登入會在下一個請求失效
            if (resetPassword && vm.UserId == User.GetId())
                ModelState.AddModelError(nameof(vm.Password), "請到前台會員中心修改自己的密碼");
            if (!ModelState.IsValid) return View(vm);

            // Email 改了：新的 Email 還沒驗證過（Google 登入接管、驗證信都以 EmailVerified 為準）
            if (!string.Equals(vm.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.EmailVerified = false;
                user.EmailVerifyToken = null;
                user.EmailVerifyTokenExpiry = null;
            }

            // 更新基本資料
            user.Username = vm.Username;
            user.Email = vm.Email;

            // --- 關鍵：更新帳號狀態 (停權/啟用) ---
            user.IsActive = vm.IsActive;

            await _userService.UpdateUserAsync(user);

            // 有填密碼才重設（原本寫入雜湊的那行被註解，畫面顯示成功但密碼沒變）；重設後該會員所有裝置登出
            if (resetPassword)
            {
                var result = await _authService.AdminSetPasswordAsync(user.UserId, vm.Password!);
                if (result != ChangePasswordResult.Success)
                {
                    ModelState.AddModelError(nameof(vm.Password), result == ChangePasswordResult.InvalidNewPassword
                        ? "密碼需為 6～100 碼" : "重設密碼失敗");
                    return View(vm);
                }
                TempData["SuccessMessage"] = "已更新會員資料並重設密碼（該會員已在所有裝置登出）";
            }

            return RedirectToAction("Index");
        }
    }
}
