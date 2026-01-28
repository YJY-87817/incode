using incode2.DTO.user;
using incode2.Extensions;
using incode2.Models;
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
  [Authorize]
    public class UserController : ControllerBase
  {
    private readonly incodedatabaseContext _context;

    public UserController(incodedatabaseContext context)
    {
      _context = context;
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

        // GET: api/User/profile?id=123
        // 如果網址有帶 ?id=123 -> 回傳該 ID 的資料 (看別人)
        // 如果網址沒帶 id      -> 回傳當前登入者的資料 (看自己)
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile([FromQuery] int? id)
        {
            // 1. 決定要查詢的目標 ID
            // 邏輯：如果參數 id 有值，就用參數 id；如果沒值，就抓 Token 裡的當前用戶 ID
            int targetId = id ?? User.GetId();

            if (targetId == 0)
            {
                return BadRequest("無法確認查詢對象");
            }

            var user = await _context.User
                .Include(u => u.Role)
                .Where(u => u.UserId == targetId) 
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    ImageUrl = u.ImageUrl,
                    Role = u.Role.RoleName ?? "Member"
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("找不到該會員資料");
            }

            return Ok(user);
        }

        // PUT: api/User/5
        // 修改資料 (只允許修改 UpdateUserDto 裡定義的欄位)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserDto dto)
        {
            // 1. 安全檢查：確保登入者只能改自己的資料 (除非是管理員)
            int currentUserId = User.GetId();
            if (id != currentUserId)
            {
                // 這裡也可以判斷如果 User.IsInRole("Admin") 則允許修改
                return Forbid();
            }   

            var user = await _context.User.FindAsync(id);
            if (user == null) return NotFound();

            // 1. Username 比較特別，通常資料庫不允許為 null，且不能是空字串
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
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
                if (!UserExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
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
    }
}
