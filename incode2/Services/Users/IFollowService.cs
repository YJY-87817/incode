using incode2.Dtos.Users;

namespace incode2.Services.Users
{
    public interface IFollowService
    {
        // 搜尋使用者 (排除自己，並標記是否已追蹤)
        Task<List<FollowingUserDto>> SearchUsersAsync(string keyword, int currentUserId);
        // 取得我的追蹤名單 (我關注了誰)
        Task<List<FollowingUserDto>> GetMyFollowingListAsync(int userId);

        Task FollowUserAsync(int currentUserId, int targetUserId);// 追蹤某人

        Task UnfollowUserAsync(int currentUserId, int targetUserId);// 取消追蹤某人
    }
}
