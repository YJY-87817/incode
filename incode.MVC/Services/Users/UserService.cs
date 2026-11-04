using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Users
{
    public class UserService : IUserService
    {
        private readonly incodedatabaseContext _context;

        public UserService(incodedatabaseContext context)
        {
            _context = context;
          
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateGoogleUserAsync(string email, string name)
        {
            var newUser = new User
            {
                Email = email,
                Username = name ?? "Google User",
                Password = "GOOGLE_AUTH", // 標記
                CreatedAt = DateTime.Now,
                IsActive = true,
                RoleId = 2, 
                ImageUrl = "https://cdn-icons-png.flaticon.com/512/847/847969.png"
            };

            _context.User.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
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

        public async Task UpdateUserAsync(User user)
        {
            _context.User.Update(user);
            await _context.SaveChangesAsync();
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