using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace incode2.ApiControllers.Users
{
  //localhost:7190/api/User
  [Route("api/[controller]")]
  [ApiController]
  [Authorize] // 預設需登入；公開的端點個別標示 [AllowAnonymous]
    public class UserController : ControllerBase
  {
    private readonly incodedatabaseContext _context;
        private readonly IUserService _userService;
        private readonly IImageService _imageService;

        public UserController(incodedatabaseContext context, IUserService userService, IImageService imageService)
        {
      _context = context;
            _userService = userService;
            _imageService = imageService;
        }

        private const int MaxAvatarFiles = 5;
        private const long MaxAvatarBytes = 5L * 1024 * 1024; // 5 MB

        // POST: api/User/images  上傳大頭貼
        // 註冊流程會在登入前先上傳大頭貼，因此允許匿名；只接受檢查過的圖片 (格式 + 檔頭)，限制數量與大小
        [HttpPost("images")]
        [AllowAnonymous]
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

            string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/user");
            Directory.CreateDirectory(storagePath);

            var urls = new List<string>();
            foreach (var file in list)
            {
                var fileName = $"{Guid.NewGuid()}{ImageFileValidator.NormalizedExtension(file)}";
                using (var stream = new FileStream(Path.Combine(storagePath, fileName), FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream);
                }
                urls.Add($"/img/user/{fileName}");
            }

            return Ok(new { imageUrls = urls });
        }

        // GET: api/User  取得所有使用者列表 (含 Email，僅限管理員)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUser()
        {
            // 使用 Select 將 User 轉成 UserDto (自動過濾掉密碼)
            return await _context.User
                .Include(u => u.Role)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    Username = u.Username,
                    ImageUrl = u.ImageUrl,
                    Role = u.Role.RoleName ?? "Member"
                })
                .ToListAsync();
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
            User targetUser = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "請先登入才能查看個人檔案" });
                }
                targetUser = await _context.User.FindAsync(currentUserId);
            }
            else
            {
                targetUser = await _context.User
                    .FirstOrDefaultAsync(u => u.Username == username);
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
            // 1. 安全檢查：確保登入者只能改自己的資料 (除非是管理員)
            int currentUserId = User.GetId();
            var user = await _context.User.FindAsync(currentUserId);
            if (user == null) return NotFound("找不到使用者");

            // 1. Username 比較特別，通常資料庫不允許為 null，且不能是空字串
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                if (await _context.User.AnyAsync(u => u.Username == dto.Username && u.UserId != currentUserId))
                {
                    return BadRequest("此暱稱已被使用");
                }
                user.Username = dto.Username;
            }

            // 2. 其他欄位：如果有值就更新，沒值(null)就保持原樣
            // 寫法解釋：user.Phone = 新值 ?? 舊值; (如果新值是null，就用舊值)

            if (dto.ImageUrl != null) user.ImageUrl = dto.ImageUrl;
            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.Address != null) user.Address = dto.Address;
            if (dto.Birthday != null) user.Birthday = dto.Birthday;
           
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

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

        // DELETE: api/User/5  刪除會員 (僅限管理員)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.UserId == id);
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
