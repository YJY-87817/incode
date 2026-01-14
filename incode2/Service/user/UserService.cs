using incode2.DTO.user;
using incode2.Models;
using incode2.Service.user;

namespace incode2.Service.user
{
    public class UserService : IUserService
    {
        private readonly incodedatabaseContext _context;
        private readonly IUserService _userService;

        public UserService(incodedatabaseContext context)
        {
            _context = context;
        }

        public User? ValidateUser(string email, string password)
        {
            // 這裡應該要有密碼雜湊的驗證，這裡簡化處理
            var user = _context.User.FirstOrDefault(u => u.Email == email && u.Password == password);
            return user;
        }

        public void Register(RegisterDto registerDto)
        {
            var user = new User
            {
                Email = registerDto.Email,
                Password = registerDto.Password,
                // 根據 RegisterDto 和 User 的定義補齊其他必要欄位
            };
            _context.User.Add(user);
            _context.SaveChanges();
        }

        public bool UserExists(string email)
        {
            return _context.User.Any(u => u.Email == email);
        }
    }
}
