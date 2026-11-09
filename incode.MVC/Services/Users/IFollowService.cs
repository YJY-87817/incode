
namespace incode.MVC.Services.Users
{
    public interface IFollowService
    {
        // 搜尋使用者 (排除自己，並標記是否已追蹤)
        Task<List<FollowingUserDto>> SearchUsersAsync(string keyword, int currentUserId);
        // 取得我的追蹤名單 (我關注了誰)
        Task<List<FollowingUserDto>> GetMyFollowingListAsync(int userId);

        Task<FollowResult> FollowUserAsync(int currentUserId, int targetUserId);// 追蹤某人

        Task<FollowResult> UnfollowUserAsync(int currentUserId, int targetUserId);// 取消追蹤某人

        // 指定會員的粉絲 / 追蹤中名單（Id 為對方的會員編號）
        Task<List<FollowListItemDto>> GetFollowersAsync(int userId);
        Task<List<FollowListItemDto>> GetFollowingAsync(int userId);
    }

    public enum FollowResult
    {
        Success,
        Self,             // 不能追蹤自己
        AlreadyFollowing, // 已經追蹤過
        TargetNotFound,   // 對方不存在
        NotFollowing      // 取消追蹤時沒有追蹤紀錄
    }
}
