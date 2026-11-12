using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace incode.MVC.ApiControllers.Users
{
  //localhost:7190/api/User
  [Route("api/[controller]")]
  [ApiController]
  [Authorize] // 預設需登入；公開的端點個別標示 [AllowAnonymous]
    public class UserController : ControllerBase
  {
        private readonly IUserService _userService;
        private readonly IImageService _imageService;
        private readonly UploadStorage _storage;

        public UserController(IUserService userService, IImageService imageService, UploadStorage storage)
        {
            _userService = userService;
            _imageService = imageService;
            _storage = storage;
        }

        private const int MaxAvatarFiles = 5;
        private const long MaxAvatarBytes = 5L * 1024 * 1024; // 5 MB

        // POST: api/User/images  上傳大頭貼
        // 註冊流程會在登入前先上傳大頭貼，因此允許匿名；只接受檢查過的圖片 (格式 + 檔頭)，限制數量與大小
        [HttpPost("images")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Upload)] // 匿名也能上傳，限制次數避免塞滿磁碟
        [RequestSizeLimit(MaxAvatarFiles * MaxAvatarBytes)]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            var list = files?.Where(f => f != null && f.Length > 0).ToList();
            if (list == null || list.Count == 0)
                return BadRequest(new { message = "未提供檔案" });
            if (list.Count > MaxAvatarFiles)
                return BadRequest(new { message = $"一次最多上傳 {MaxAvatarFiles} 張圖片" });

            foreach (var file in list)
            {
                var error = ImageFileValidator.Validate(file, MaxAvatarBytes);
                if (error != null) return BadRequest(new { message = error });
            }

            var urls = new List<string>();
            foreach (var file in list)
            {
                var fileName = $"{Guid.NewGuid()}{ImageFileValidator.NormalizedExtension(file)}";
                urls.Add(await _storage.SaveAsync(UploadFolders.Avatars, fileName, file));
            }

            return Ok(new { imageUrls = urls });
        }

        // GET: api/User  取得所有使用者列表 (含 Email，僅限管理員)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUser()
        {
            // 回傳 UserDto（不含密碼等敏感欄位）
            return await _userService.GetUserListAsync();
        }

        // GET: api/User/profile?username={username}
        // 不帶 username 取得自己的完整資料 (需登入)；帶 username 為公開個人頁，只回傳公開欄位
        [HttpGet("profile")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> GetProfile([FromQuery] string? username)
        {
            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {               
                currentUserId = User.GetId();
            }
            User? targetUser;

            if (string.IsNullOrWhiteSpace(username))
            {
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "請先登入才能查看個人檔案" });
                }
                targetUser = await _userService.GetUserByIdAsync(currentUserId.Value);
            }
            else
            {
                targetUser = await _userService.GetUserByUsernameAsync(username);
            }
                        
            if (targetUser == null)
            {
                return NotFound(new { message = "找不到該使用者" });
            }

            var responseDto = new UserDto
            {
                UserId = targetUser.UserId,
                Username = targetUser.Username,
                ImageUrl = targetUser.ImageUrl ?? "",                                                      
            };

            bool isMe = currentUserId.HasValue && currentUserId.Value == targetUser.UserId;

            if (isMe)
            {
                responseDto.Email = targetUser.Email;
                responseDto.Phone = targetUser.Phone;
                responseDto.Address = targetUser.Address;
                responseDto.Birthday = targetUser.Birthday;
            }

            return Ok(responseDto);        
        }

        // PUT: api/User/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserDto dto)
        {
            // 只能改自己的資料：ID 一律取自登入者
            var (result, user) = await _userService.UpdateProfileAsync(User.GetId(), dto);
            if (result == UpdateProfileResult.NotFound) return NotFound("找不到使用者");
            if (result == UpdateProfileResult.UsernameTaken) return BadRequest("此暱稱已被使用");
            user = user!;

            // 只回傳可公開給本人的欄位 (原本回傳整個 User 實體，包含密碼雜湊與 ResetToken)
            return Ok(new
            {
                message = "更新成功",
                user = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    ImageUrl = user.ImageUrl ?? "",
                    Email = user.Email,
                    Phone = user.Phone,
                    Address = user.Address,
                    Birthday = user.Birthday,
                }
            });
        }        

        // POST: api/User/5/deactivate  停用帳號 (僅限管理員)
        // 不刪除資料（會員有任務進度、點數、文章等關聯資料）：停用後無法登入，已登入的人下一個請求就被登出
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            if (id == User.GetId())
                return BadRequest(new { message = "不能停用自己的帳號" });

            if (!await _userService.SetActiveAsync(id, false))
                return NotFound();

            return Ok(new { message = "已停用帳號" });
        }

        // POST: api/User/5/activate  恢復帳號 (僅限管理員)
        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            if (!await _userService.SetActiveAsync(id, true))
                return NotFound();

            return Ok(new { message = "已恢復帳號" });
        }

        // GET: api/User/41/stats  公開個人頁的統計數字
        [HttpGet("{userId}/stats")]
        [AllowAnonymous]
        public async Task<ActionResult<UserProfileStatsDto>> GetUserStats(int userId)
        {
            // 呼叫 Service
            var stats = await _userService.GetUserStatsAsync(userId);

            // 判斷 Service 是否回傳 null (代表找不到該用戶)
            if (stats == null)
            {
                return NotFound(new { message = "找不到該使用者或是使用者不存在" });
            }

            // 回傳 200 OK 與數據
            return Ok(new
            {
                articleCount = stats.ArticleCount,
                followers = stats.FollowerCount, 
                following = stats.FollowingCount,
                missionCount = stats.MissionCount,
                points = stats.PointBalance
            });
        }
    }
}
