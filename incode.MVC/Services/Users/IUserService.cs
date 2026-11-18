
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

        /// <summary>所有會員各自最近 count 筆登入紀錄（一次查詢；後台會員列表用，原本每位會員各查一次）。</summary>
        Task<Dictionary<int, List<LoginLogDto>>> GetRecentLoginLogsForAllUsersAsync(int count);

        /// <summary>Email / 暱稱是否已被其他會員使用（後台編輯會員時檢查，兩個欄位都有唯一索引）。</summary>
        Task<bool> IsEmailTakenByOtherAsync(string email, int exceptUserId);
        Task<bool> IsUsernameTakenByOtherAsync(string username, int exceptUserId);

        // 更新使用者 (存檔用)
        Task UpdateUserAsync(User user);
        Task<User> CreateGoogleUserAsync(string email, string? name);
        Task AddLoginLogAsync(int userId, string ipAddress);
        Task<UserProfileStatsDto> GetUserStatsAsync(int userId);

        /// <summary>管理員用的會員列表（不含密碼等敏感欄位）。</summary>
        Task<List<UserDto>> GetUserListAsync();

        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 更新會員自己的資料：暱稱有填才改（不可與其他人重複），其他欄位為 null 時保持原值。
        /// </summary>
        Task<(UpdateProfileResult Result, User? User)> UpdateProfileAsync(int userId, UpdateUserDto dto);

        /// <summary>
        /// 停用 / 啟用帳號（不刪除資料）；停用後無法登入，已登入的人也會被登出。找不到回傳 false。
        /// </summary>
        Task<bool> SetActiveAsync(int userId, bool isActive);
    }

    public enum UpdateProfileResult
    {
        Success,
        NotFound,
        UsernameTaken,
        InvalidImageUrl,
        InvalidUsername
    }
}
