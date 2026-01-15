using incode2.DTO.user;
using incode2.Models;
using incode2.Service.user.@interface;
using Microsoft.EntityFrameworkCore;

namespace incode2.Service.user
{
    public class SocialService : ISocialService
    {
        private readonly incodedatabaseContext _context;

        public SocialService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 1. 搜尋使用者
        public List<UserSummaryDto> SearchUsers(string keyword, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<UserSummaryDto>();

            // 搜尋 username 或 email 包含關鍵字的人，且排除自己
            var query = _context.User
                .Where(u => u.UserId != currentUserId &&
                           (u.Username.Contains(keyword) || u.Email.Contains(keyword)))
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    ImageUrl = u.ImageUrl,
                    // 檢查 Follow 表：我是 Follower (追隨者)，對方是 Followee (被追隨者)
                    IsFollowing = _context.Follow.Any(f => f.FollowerId == currentUserId && f.FolloweeId == u.UserId)
                });

            return query.ToList();
        }

        // 2. 取得我追蹤的名單
        public List<UserSummaryDto> GetMyFollowingList(int currentUserId)
        {
            // 從 Follow 表出發，找出 FollowerId 是我的人，並關聯出對方(Followee)的資料
            var list = _context.Follow
                .Where(f => f.FollowerId == currentUserId)
                .Include(f => f.Followee) // 載入被追蹤者的資料
                .Select(f => new UserSummaryDto
                {
                    UserId = f.Followee.UserId,
                    Username = f.Followee.Username,
                    Email = f.Followee.Email,
                    ImageUrl = f.Followee.ImageUrl,
                    IsFollowing = true // 既然在追蹤名單內，當然是 true
                })
                .ToList();

            return list;
        }

        // 3. 追蹤使用者
        public void FollowUser(int currentUserId, int targetUserId)
        {
            // 防止追蹤自己
            if (currentUserId == targetUserId) return;

            // 檢查是否已經追蹤過
            bool exists = _context.Follow.Any(f => f.FollowerId == currentUserId && f.FolloweeId == targetUserId);
            if (exists) return;

            var follow = new Follow
            {
                FollowerId = currentUserId, // 我是粉絲
                FolloweeId = targetUserId,  // 對方是被關注者
                CreatedAt = DateTime.Now
            };

            _context.Follow.Add(follow);
            _context.SaveChanges();
        }

        // 4. 取消追蹤
        public void UnfollowUser(int currentUserId, int targetUserId)
        {
            var follow = _context.Follow
                .FirstOrDefault(f => f.FollowerId == currentUserId && f.FolloweeId == targetUserId);

            if (follow != null)
            {
                _context.Follow.Remove(follow);
                _context.SaveChanges();
            }
        }
    }
}

