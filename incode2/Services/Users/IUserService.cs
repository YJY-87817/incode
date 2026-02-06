using incode2.DTO.user;
using incode2.Dtos.Users;
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
        Task<bool> IsUsernameExistAsync(string username);

        Task<List<User>> GetAllUsersAsync();

        // 新增：依據 User ID 取得登入紀錄 (參數 count 用來決定要抓最近幾筆)
        Task<List<LoginLogDto>> GetLoginLogsByUserIdAsync(int userId, int count);              

        // 更新使用者 (存檔用)
        Task UpdateUserAsync(User user);

        Task AddLoginLogAsync(int userId, string ipAddress);
        Task<UserProfileStatsDto> GetUserStatsAsync(int userId);
    }
}
