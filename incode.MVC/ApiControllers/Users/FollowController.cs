using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;

        public FollowController(IFollowService followService)
        {
            _followService = followService;
        }

        // GET: api/Follow/followers  我的粉絲
        [HttpGet("followers")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyFollowers()
        {
            var list = await _followService.GetFollowersAsync(User.GetId());
            return Ok(list.Select(x => new { FollowerId = x.Id, x.Username, x.ImageUrl, x.CreatedAt }));
        }

        // GET: api/Follow/following  我追蹤中的人
        [HttpGet("following")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyFollowing()
        {
            var list = await _followService.GetFollowingAsync(User.GetId());
            return Ok(list.Select(x => new { FolloweeId = x.Id, x.Username, x.ImageUrl, x.CreatedAt }));
        }

        // GET: api/Follow/followers/5  指定會員的粉絲（Id 為粉絲的會員編號）
        [HttpGet("followers/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowListItemDto>>> GetUserFollowers(int userId)
            => Ok(await _followService.GetFollowersAsync(userId));

        // GET: api/Follow/following/5  指定會員追蹤中的人（Id 為被追蹤者的會員編號）
        [HttpGet("following/{userId}")]
        public async Task<ActionResult<IEnumerable<FollowListItemDto>>> GetUserFollowing(int userId)
            => Ok(await _followService.GetFollowingAsync(userId));

        // POST: api/Follow/{targetId}
        [HttpPost("{targetId}")]
        public async Task<IActionResult> FollowUser(int targetId)
        {
            var result = await _followService.FollowUserAsync(User.GetId(), targetId);
            return result switch
            {
                FollowResult.Success => Ok(new { message = "追蹤成功" }),
                FollowResult.Self => BadRequest("不能追蹤自己"),
                FollowResult.AlreadyFollowing => BadRequest("已經追蹤過了"),
                FollowResult.TargetNotFound => NotFound("找不到該使用者"),
                _ => StatusCode(500)
            };
        }

        // DELETE: api/Follow/{targetId}
        [HttpDelete("{targetId}")]
        public async Task<IActionResult> UnfollowUser(int targetId)
        {
            var result = await _followService.UnfollowUserAsync(User.GetId(), targetId);
            return result == FollowResult.Success
                ? Ok(new { message = "已取消追蹤" })
                : NotFound("未找到追蹤紀錄");
        }
    }
}
