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

    }
}
