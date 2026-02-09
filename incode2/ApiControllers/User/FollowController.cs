using incode2.Models;
using incode2.Extensions; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.ApiController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public FollowController(incodedatabaseContext context)
        {
            _context = context;
        }

        // GET: api/Follow/followers
        [HttpGet("followers")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyFollowers()
        {            
            int myId = User.GetId();

            var followers = await _context.Follow
                .Where(f => f.FolloweeId == myId)
                .Select(f => new {
                    f.FollowerId,
                    f.Follower.Username,
                    f.Follower.ImageUrl,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(followers);
        }

        // GET: api/Follow/following
        [HttpGet("following")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyFollowing()
        {
            int myId = User.GetId(); 

            var following = await _context.Follow
                .Where(f => f.FollowerId == myId)
                .Select(f => new {
                    f.FolloweeId,
                    f.Followee.Username,
                    f.Followee.ImageUrl,
                    f.CreatedAt
                })
                .ToListAsync();

            return Ok(following);
        }

        // GET: api/Follow/followers/5
        [HttpGet("followers/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserFollowers(int userId)
        {
            var followers = await _context.Follow
                .Where(f => f.FolloweeId == userId) // 這裡改用傳進來的 userId
                .Select(f => new {
                    Id = f.FollowerId,              // 注意：這裡回傳的是粉絲的 ID
                    Username = f.Follower.Username,
                    ImageUrl = f.Follower.ImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(followers);
        }

        // GET: api/Follow/following/5
        [HttpGet("following/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserFollowing(int userId)
        {
            var following = await _context.Follow
                .Where(f => f.FollowerId == userId) // 這裡改用傳進來的 userId
                .Select(f => new {
                    Id = f.FolloweeId,              // 注意：這裡回傳的是被追蹤者的 ID
                    Username = f.Followee.Username,
                    ImageUrl = f.Followee.ImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(following);
        }

        // POST: api/Follow/{targetId}
        [HttpPost("{targetId}")]
        public async Task<IActionResult> FollowUser(int targetId)
        {
            int myId = User.GetId(); 

            if (myId == targetId) return BadRequest("不能追蹤自己");

            var exists = await _context.Follow
                .AnyAsync(f => f.FollowerId == myId && f.FolloweeId == targetId);

            if (exists) return BadRequest("已經追蹤過了");

            var follow = new Follow
            {
                FollowerId = myId,
                FolloweeId = targetId,
                CreatedAt = DateTime.Now
            };

            _context.Follow.Add(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "追蹤成功" });
        }

        // DELETE: api/Follow/{targetId}
        [HttpDelete("{targetId}")]
        public async Task<IActionResult> UnfollowUser(int targetId)
        {
            int myId = User.GetId(); 

            var follow = await _context.Follow
                .FirstOrDefaultAsync(f => f.FollowerId == myId && f.FolloweeId == targetId);

            if (follow == null) return NotFound("未找到追蹤紀錄");

            _context.Follow.Remove(follow);
            await _context.SaveChangesAsync();

            return Ok(new { message = "已取消追蹤" });
        }
    }
}