using incode2.DTO.user;

namespace incode2.Service.user.@interface
{
    public interface ISocialService
    {        
        // 搜尋使用者 (排除自己，並標記是否已追蹤)
        List<UserSummaryDto> SearchUsers(string keyword, int currentUserId);

        // 取得我的追蹤名單 (我關注了誰)
        List<UserSummaryDto> GetMyFollowingList(int currentUserId);

        void FollowUser(int currentUserId, int targetUserId);// 追蹤某人

        void UnfollowUser(int currentUserId, int targetUserId);// 取消追蹤某人
    }
}
