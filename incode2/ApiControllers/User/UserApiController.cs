using incode2.DTO.user;
using incode2.Extensions;
using incode2.Models;
using incode2.Services;
using incode2.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace incode2.ApiController.user
{
  //localhost:7190/api/User
  [Route("api/[controller]")]
  [ApiController]
  //[Authorize]
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

        [HttpPost("images")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                    return BadRequest(new { message = "未提供檔案" });

                var urls = new List<string>();

                // 1. 直接在這裡指定要存入的實體路徑為 wwwroot/img/user
                string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/user");

                // 2. 檢查資料夾是否存在，沒有的話就建立
                if (!Directory.Exists(storagePath))
                    Directory.CreateDirectory(storagePath);

                // 3. 處理每一個上傳的檔案
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    // 產生唯一檔名
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(storagePath, fileName);

                    // 儲存檔案到硬碟
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // 將前端需要的網址路徑加入 List 
                    // 這裡回傳的就會是 /img/user/xxx.jpg
                    urls.Add($"/img/user/{fileName}");
                }

                if (urls.Count == 0)
                    return BadRequest(new { message = "沒有有效的檔案" });

                // 4. 回傳給前端
                return Ok(new { imageUrls = urls });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }   


// GET: api/User
// 取得所有使用者列表
// 管理員能用(或者用於搜尋)
        [HttpGet]
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

        // GET: api/User/profile??username={test2CQ}
        [HttpGet("profile")]
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

            return Ok(new { message = "更新成功", user = user });
        }        

        // DELETE: api/User/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 權限檢查：只有本人或管理員可以刪除
            int currentUserId = User.GetId();
            // 假設只有 Admin 才能隨便刪人，或者本人刪除自己
            // if (currentUserId != id && !User.IsInRole("Admin")) return Forbid();

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

        // user/41/stats
        [HttpGet("{userId}/stats")]
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
