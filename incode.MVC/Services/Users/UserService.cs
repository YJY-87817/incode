using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace incode.MVC.Services.Users
{
    public class UserService : IUserService
    {
        private readonly incodedatabaseContext _context;
        private readonly IMemoryCache _cache;

        public UserService(incodedatabaseContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email == email);
        }

        // username 欄位上限（資料庫 nvarchar(150)）
        internal const int UsernameMaxLength = 150;
        private const int GoogleUserCreateAttempts = 5;

        public async Task<User> CreateGoogleUserAsync(string email, string? name)
        {
            // username 有唯一索引：Google 顯示名稱重複（或都沒有名稱）時改用「名稱_數字」，
            // 原本直接寫入，第二位同名的使用者建立帳號會 500
            string baseName = string.IsNullOrWhiteSpace(name) ? "Google User" : name.Trim();

            for (int attempt = 0; ; attempt++)
            {
                var newUser = new User
                {
                    Email = email,
                    Username = await GetAvailableUsernameAsync(baseName),
                    Password = "GOOGLE_AUTH", // 標記
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    RoleId = 2,
                    ImageUrl = "https://cdn-icons-png.flaticon.com/512/847/847969.png"
                };

                _context.User.Add(newUser);
                try
                {
                    await _context.SaveChangesAsync();
                    return newUser;
                }
                catch (DbUpdateException) when (attempt < GoogleUserCreateAttempts - 1)
                {
                    // 同時有另一個請求搶先用了同一個名稱（或同一個 Email 同時登入）：移除這筆後重試
                    _context.Entry(newUser).State = EntityState.Detached;

                    var existing = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
                    if (existing != null) return existing;
                }
            }
        }

        // 回傳還沒被使用的 username：先用原名稱，被用掉就加上「_2」「_3」…（必要時截短，總長度不超過欄位上限）
        private async Task<string> GetAvailableUsernameAsync(string baseName)
        {
            if (baseName.Length > UsernameMaxLength)
                baseName = baseName[..UsernameMaxLength];

            if (!await IsUsernameExistAsync(baseName))
                return baseName;

            for (int n = 2; n < 100; n++)
            {
                string candidate = WithSuffix(baseName, $"_{n}");
                if (!await IsUsernameExistAsync(candidate))
                    return candidate;
            }

            // 同名的人非常多：改用隨機字尾
            return WithSuffix(baseName, "_" + Guid.NewGuid().ToString("N")[..8]);
        }

        private static string WithSuffix(string baseName, string suffix)
        {
            int keep = Math.Min(baseName.Length, UsernameMaxLength - suffix.Length);
            return baseName[..keep] + suffix;
        }

        public async Task<List<User>> SearchUsersAsync(string keyword)
        {
            // 搜尋 Email 或 Username
            var query = _context.User.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(u => u.Email.Contains(keyword) || u.Username.Contains(keyword));
            }

            return await query.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.User.FindAsync(id);
        }

        // 檢查 Email 是否存在
        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _context.User.AnyAsync(u => u.Email == email);
        }

        // 新增使用者
        public async Task CreateUserAsync(User user)
        {
            _context.User.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUsernameExistAsync(string username)
        {
            return await _context.User.AnyAsync(u => u.Username == username);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {           
            return await _context.User
                                 .OrderByDescending(u => u.CreatedAt) // 建議排序
                                 .ToListAsync();
        }

        // 2. 實作：取得登入紀錄
        public async Task<List<LoginLogDto>> GetLoginLogsByUserIdAsync(int userId, int count)
        {            
            return await _context.UserLoginlog
                                 .Where(log => log.UserId == userId) // 篩選該使用者的紀錄
                                 .OrderByDescending(log => log.Logintime) // 最新時間在最上面
                                 .Take(count) // 只抓前 N 筆 (例如前 10 筆)
                                 .Select(log => new LoginLogDto
                                 {
                                     // 左邊是 DTO 的欄位 = 右邊是資料庫 Entity 的欄位

                                     // 注意：因為資料庫 Logintime 是 DateTime? (可空)，DTO 是 DateTime (不可空)
                                     // 所以要用 .GetValueOrDefault() 或 ?? DateTime.MinValue 處理 null
                                     LoginTime = log.Logintime.GetValueOrDefault(),

                                     IpAddress = log.IpAddress,

                                     // IsSuspicious 資料庫沒有，先給預設值 false，
                                     // 真正的判斷邏輯通常由 Controller 拿到資料後再計算，或是在這裡寫簡單邏輯
                                     IsSuspicious = false
                                 })
                                 .ToListAsync();
        }               

        public async Task<List<UserDto>> GetUserListAsync()
        {
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

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<(UpdateProfileResult Result, User? User)> UpdateProfileAsync(int userId, UpdateUserDto dto)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return (UpdateProfileResult.NotFound, null);

            // Username 不能是空字串；有填才改，且不能與其他人重複
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                if (await _context.User.AnyAsync(u => u.Username == dto.Username && u.UserId != userId))
                    return (UpdateProfileResult.UsernameTaken, null);
                user.Username = dto.Username;
            }

            // 其他欄位：有值就更新，null 保持原值
            if (dto.ImageUrl != null) user.ImageUrl = dto.ImageUrl;
            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.Address != null) user.Address = dto.Address;
            if (dto.Birthday != null) user.Birthday = dto.Birthday;

            await _context.SaveChangesAsync();
            return (UpdateProfileResult.Success, user);
        }

        public async Task<bool> SetActiveAsync(int userId, bool isActive)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = isActive;
            await _context.SaveChangesAsync();
            MemberCookieValidator.Evict(_cache, userId); // 讓登入狀態檢查立即看到新狀態
            return true;
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.User.Update(user);
            await _context.SaveChangesAsync();
            MemberCookieValidator.Evict(_cache, user.UserId); // 後台會員編輯可能改了 IsActive
        }
        public async Task AddLoginLogAsync(int userId, string ipAddress)
        {
            var log = new UserLoginlog
            {
                UserId = userId,
                IpAddress = ipAddress,
                Logintime = DateTime.Now 
            };

            _context.UserLoginlog.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<UserProfileStatsDto> GetUserStatsAsync(int userId)
        {
            bool userExists = await _context.User
            .AnyAsync(u => u.UserId == userId);

            if (!userExists)
            {
                return null; // 回傳 null 讓 Controller 報 404
            }    

            var articleCountTask = await _context.Content
                .CountAsync(c => c.UserId == userId
                  && c.IsActive == 1     
                  && c.Type == 1);

            var missionCountTask = await _context.MissionProgress
    .CountAsync(mp => mp.UserId == userId
                   && (mp.Status == "completed" || mp.Status == "rewarded"));

            var pointBalanceTask = await _context.PointLog
            .Where(p => p.UserId == userId &&
                       (p.Status == "success" || p.Status == "system")) 
            .SumAsync(p => p.ChangeAmount);

            var followingCountTask = await _context.Follow
                .CountAsync(f => f.FollowerId == userId);

            var followerCountTask = await _context.Follow
                .CountAsync(f => f.FolloweeId == userId);
            
            return new UserProfileStatsDto
            {
                ArticleCount = articleCountTask,
                MissionCount = missionCountTask,               
                PointBalance = pointBalanceTask,
                FollowingCount = followingCountTask,
                FollowerCount = followerCountTask
            };
        }
    }
}