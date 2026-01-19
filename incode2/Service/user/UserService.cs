using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Service.user
{
    public class UserService : IUserService
    {
        private readonly incodedatabaseContext _context;
        private readonly incodedatabaseContext _userService;

        public UserService(incodedatabaseContext context)
        {
            _context = context;
            _userService

        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email == email);
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

        // 檢查 Email 是否存在 (單純的資料查詢)
        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _context.User.AnyAsync(u => u.Email == email);
        }

        // 新增使用者 (單純的資料寫入)
        public async Task CreateUserAsync(User user)
        {
            _context.User.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<int> LoginProcess(string email, string password)
        {
            // 1. 使用你提供的 Service 方法查詢使用者
            var user = await _userService.GetUserByEmailAsync(email);

            if (user != null)
            {
                // 2. 驗證密碼 (這裡省略雜湊比對邏輯)
                // if (VerifyPassword(password, user.Password)) ...

                // 3. 取得 UserID
                // 注意：這裡的 "Id" 取決於你的 User Model 定義，通常是 Id, UserId 或 UID
                int userId = user.Id;

                return userId;
            }
            return 0; // 找不到使用者
        }
    }
}