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

        // 追蹤使用者
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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                // 連點兩下：同時送出的另一個請求已經先追蹤了（主鍵 follower_id + followee_id），原本回 500
                _context.ChangeTracker.Clear();
                return FollowResult.AlreadyFollowing;
            }
            return FollowResult.Success;
        }

        // 4. 取消追蹤
        public async Task<FollowResult> UnfollowUserAsync(int currentUserId, int targetUserId)
        {
            var follow = await _context.Follow
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == targetUserId);

            if (follow == null) return FollowResult.NotFollowing;

            _context.Follow.Remove(follow);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // 同時送出的另一個請求已經先取消追蹤了
                _context.ChangeTracker.Clear();
                return FollowResult.NotFollowing;
            }
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

