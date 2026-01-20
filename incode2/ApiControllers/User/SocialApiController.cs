using incode2.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.ApiControllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  //登入後才能使用

    public class SocialApiController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public SocialApiController(ISocialService SocialService)
        {
            _socialService = SocialService;
        }

        // 取得當前登入者 ID 的輔助方法
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        // GET: api/Social/search?keyword=小明
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });
                               
                var result = await _socialService.SearchUsersAsync(keyword, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Social/following 看我自己的關注名單
        [HttpGet("following")]
        public async Task<IActionResult> GetFollowing()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

            var result = await _socialService.GetMyFollowingListAsync(userId);
            return Ok(result);
        }

        // POST: api/Social/user/5/following 看別人的關注名單
        [HttpGet("user/{targetUserId}/following")]
        public async Task<IActionResult> GetUserFollowing(int targetUserId)
        {
            // 這裡是用網址傳進來的 ID (targetUserId) 去查，而不是查登入者
            var result = await _socialService.GetMyFollowingListAsync(targetUserId);

            return Ok(result);
        }

        // POST: api/Social/unfollow/5
        [HttpPost("unfollow/{targetId}")]
        public async Task<IActionResult> Unfollow(int targetId)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

                await _socialService.UnfollowUserAsync(userId, targetId);
                return Ok(new { message = "已取消追蹤" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "操作失敗", error = ex.Message });
            }
        }
    }
}
