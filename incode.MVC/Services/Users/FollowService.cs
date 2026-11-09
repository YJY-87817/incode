using Microsoft.EntityFrameworkCore;

namespace incode.MVC.Services.Users
{
    public class FollowService : IFollowService
    {
        private readonly incodedatabaseContext _context;

        public FollowService(incodedatabaseContext context)
        {
            _context = context;
        }

        // 1. 搜尋使用者
        public async Task<List<FollowingUserDto>> SearchUsersAsync(string keyword, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<FollowingUserDto>();

            // 搜尋 username 或 email 包含關鍵字的人，且排除自己
            var query = _context.User
                .Where(u => u.UserId != currentUserId &&
                           (u.Username.Contains(keyword) || u.Email.Contains(keyword)))
                .Select(u => new FollowingUserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    ImageUrl = u.ImageUrl,
                    // 檢查 Follow 表：我是 Follower (追隨者)，對方是 Followee (被追隨者)
                    IsFollowing = _context.Follow.Any(f => f.FollowerId == currentUserId && f.FolloweeId == u.UserId)
                });

            return await query.ToListAsync();
        }

        // 2. 取得我追蹤的名單
        public async Task<List<FollowingUserDto>> GetMyFollowingListAsync(int currentUserId)
        {
            // 從 Follow 表出發，找出 FollowerId 是我的人，並關聯出對方(Followee)的資料
            var list = _context.Follow
                .Where(f => f.FollowerId == currentUserId)
                .Include(f => f.Followee) // 載入被追蹤者的資料
                .Select(f => new FollowingUserDto
                {
                    UserId = f.Followee.UserId,
                    Username = f.Followee.Username,
                    Email = f.Followee.Email,
                    ImageUrl = f.Followee.ImageUrl,
                    IsFollowing = true // 既然在追蹤名單內，當然是 true
                });
            return await list.ToListAsync();
        }

        // 3. 追蹤使用者
        public async Task<FollowResult> FollowUserAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId) return FollowResult.Self;

            if (!await _context.User.AnyAsync(u => u.UserId == targetUserId))
                return FollowResult.TargetNotFound;

            bool exists = await _context.Follow.AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == targetUserId);
            if (exists) return FollowResult.AlreadyFollowing;

            _context.Follow.Add(new Follow
            {
                FollowerId = currentUserId, // 我是粉絲
                FolloweeId = targetUserId,  // 對方是被關注者
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return FollowResult.Success;
        }

        // 4. 取消追蹤
        public async Task<FollowResult> UnfollowUserAsync(int currentUserId, int targetUserId)
        {
            var follow = await _context.Follow
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == targetUserId);

            if (follow == null) return FollowResult.NotFollowing;

            _context.Follow.Remove(follow);
            await _context.SaveChangesAsync();
            return FollowResult.Success;
        }

        // 5. 指定會員的粉絲名單
        public Task<List<FollowListItemDto>> GetFollowersAsync(int userId) =>
            _context.Follow
                .Where(f => f.FolloweeId == userId)
                .Select(f => new FollowListItemDto
                {
                    Id = f.FollowerId,
                    Username = f.Follower.Username,
                    ImageUrl = f.Follower.ImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

        // 6. 指定會員追蹤中的名單
        public Task<List<FollowListItemDto>> GetFollowingAsync(int userId) =>
            _context.Follow
                .Where(f => f.FollowerId == userId)
                .Select(f => new FollowListItemDto
                {
                    Id = f.FolloweeId,
                    Username = f.Followee.Username,
                    ImageUrl = f.Followee.ImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
    }
}

