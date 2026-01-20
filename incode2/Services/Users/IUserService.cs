using incode2.Models;

namespace incode2.Services.Users
{
    public interface IUserService
    {
        Task<List<User>> SearchUsersAsync(string keyword);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> IsEmailExistAsync(string email);
        Task CreateUserAsync(User user);
    }
}
