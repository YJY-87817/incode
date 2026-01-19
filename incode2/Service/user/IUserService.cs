using incode2.Models;

namespace incode2.Service.user
{
    public interface IUserService
    {
        Task<List<User>> SearchUsersAsync(string keyword);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> IsEmailExistAsync(string email);
        Task CreateUserAsync(User user);
        Task<int> LoginProcess(string email, string password);
    }
}
