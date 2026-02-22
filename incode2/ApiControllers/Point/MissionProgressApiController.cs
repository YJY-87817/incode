using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissionProgressApiController : ControllerBase
    {
        private readonly IMissionProgressService _service;

        public MissionProgressApiController(IMissionProgressService service)
        {
            _service = service;
        }

        [HttpGet("my-missions")]
        [Authorize]
        public async Task<IActionResult> GetMyMissions()
        {
            int userId = User.GetId();
            if (userId <= 0) return Unauthorized();

            var missions = await _service.GetUserMissionsAsync(userId);
            return Ok(missions);
        }

        [HttpPost("{missionId}/increment")]
        [Authorize]
        public async Task<IActionResult> Increment(int missionId, [FromBody] IncrementRequest? req)
        {
            int userId = User.GetId();
            var increment = req?.Increment ?? 1;

            var result = await _service.IncrementProgressAsync(userId, missionId, increment);
            return StatusCode(result.Code, result);
        }

        [HttpPost("claim-reward")]
        [Authorize]
        public async Task<ActionResult<ResultDTO>> ClaimReward([FromBody] ClaimRewardRequest req)
        {
            int userId = User.GetId();
            if (userId <= 0)
                return Unauthorized(new ResultDTO { Ok = false, Code = 401, Message = "使用者身份無效" });

            var result = await _service.ClaimRewardAsync(userId, req.ProgressId);
            return StatusCode(result.Code, result);
        }

        public class ClaimRewardRequest
        {
            public int ProgressId { get; set; }  // MissionProgressId
        }
    }

    public class IncrementRequest { public int Increment { get; set; } = 1; }
}
