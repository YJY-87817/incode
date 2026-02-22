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
    public class MissionApiController : ControllerBase
    {
        private readonly IMissionService _missionService;

        public MissionApiController(IMissionService missionService)
        {
            _missionService = missionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMissions()
        {
            var missions = await _missionService.GetAllMissionAsync();
            return Ok(missions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMission(int id)
        {
            var mission = await _missionService.GetMissionByIdAsync(id);
            if (mission == null) return NotFound();
            return Ok(mission);
        }

        [HttpPost("{missionId}/claim")]
        [Authorize]
        public async Task<IActionResult> ClaimMission(int missionId)
        {
            int userId = User.GetId();
            if (userId <= 0) return Unauthorized();

            var result = await _missionService.ClaimMissionAsync(missionId, userId);
            return StatusCode(result.Code, result);
        }
    }
}
