using incode2.Service.user;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace incode2.ApiController.user
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "2")]  //登入後才能使用

    public class SocialController : ControllerBase
    {
        private readonly ISocialService _SocialService;

        public SocialController(ISocialService SocialService)
        {
            _SocialService = SocialService;
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
            //throw new UnauthorizedAccessException("無法識別使用者身分");
        }

        // GET: api/Social/search?keyword=小明
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string keyword)
        {
            try
            {               
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

                var result = _SocialService.SearchUsers(keyword, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Social/following
        [HttpGet("following")]
        public IActionResult GetFollowing()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

            var result = _SocialService.GetMyFollowingList(userId);
            return Ok(result);
        }

        // POST: api/Social/follow/5
        // 追蹤 ID 為 5 的使用者
        [HttpPost("follow/{targetId}")]
        public IActionResult Follow(int targetId)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

                _SocialService.FollowUser(userId, targetId);
                return Ok(new { message = "追蹤成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "操作失敗", error = ex.Message });
            }
        }

        // POST: api/Social/unfollow/5
        // 取消追蹤 ID 為 5 的使用者
        [HttpPost("unfollow/{targetId}")]
        public IActionResult Unfollow(int targetId)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == 0) return Unauthorized(new { message = "無法識別使用者身分" });

                _SocialService.UnfollowUser(userId, targetId);
                return Ok(new { message = "已取消追蹤" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "操作失敗", error = ex.Message });
            }
        }
    }
}
