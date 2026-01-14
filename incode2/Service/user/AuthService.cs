using incode2.DTO.user;
using incode2.Models;
using Microsoft.AspNetCore.Identity;

namespace incode2.Service.user
{
       public class AuthService : IUserService
    { 
        private readonly incodedatabaseContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(incodedatabaseContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();// 密碼雜湊
        }

        public void Register(RegisterDto registerDto)
        {
            // 建立新的使用者物件
            var newUser = new User
            {
                Email = registerDto.Email,
                Username = registerDto.Username,
                RoleId = 2, 
            };
            string hashedPassword = _passwordHasher.HashPassword(newUser, registerDto.Password);
            newUser.Password = hashedPassword;
            _context.User.Add(newUser);
            _context.SaveChanges();
        }
        public User ValidateUser(string email, string password)
        {            
            var user = _context.User.FirstOrDefault(u => u.Email == email);
            
            if (user == null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Success)
            {
                return user; // 密碼正確，回傳使用者
            }
            else
            {
                return null; // 密碼錯誤
            }
        }

        public bool UserExists(string email)
        {
            return _context.User.Any(u => u.Email == email);
        }
    }
}
