using incode2.Models;
using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Users
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

        public async Task<bool> IsUsernameExistAsync(string username)
        {
            return await _context.User.AnyAsync(u => u.Username == username);
        }
    }
}