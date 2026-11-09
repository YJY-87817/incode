
namespace incode.MVC.Services.Users
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
        Task<User> CreateGoogleUserAsync(string email, string name);
        Task AddLoginLogAsync(int userId, string ipAddress);
        Task<UserProfileStatsDto> GetUserStatsAsync(int userId);

        /// <summary>管理員用的會員列表（不含密碼等敏感欄位）。</summary>
        Task<List<UserDto>> GetUserListAsync();

        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 更新會員自己的資料：暱稱有填才改（不可與其他人重複），其他欄位為 null 時保持原值。
        /// </summary>
        Task<(UpdateProfileResult Result, User? User)> UpdateProfileAsync(int userId, UpdateUserDto dto);

        /// <summary>刪除會員；找不到回傳 false。</summary>
        Task<bool> DeleteUserAsync(int userId);
    }

    public enum UpdateProfileResult
    {
        Success,
        NotFound,
        UsernameTaken
    }
}
