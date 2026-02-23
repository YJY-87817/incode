using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using incode2.DTO;
using incode2.Dtos;
using incode2.Extensions;
using incode2.Models;
using incode2.Partials;
using incode2.Services.Point;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode2.ApiControllers.Point
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntrustMissionProgressApiController : ControllerBase
    {
        private readonly incodedatabaseContext _context;
        private readonly IEntrustMissionProgressService _entrustMissionProgressService;

        public EntrustMissionProgressApiController(incodedatabaseContext context,
          IEntrustMissionProgressService entrustMissionProgressService)
        {
            _context = context;
            _entrustMissionProgressService = entrustMissionProgressService;
        }

        // GET: api/EntrustMissionProgressApi
        [HttpGet]
        public async Task<IEnumerable<EntrustMissionProgressDTO>> GetEntrustMissionProgress()
        {
            var entrustMissionProgress = await _entrustMissionProgressService.GetAllProgressAsync();
            return entrustMissionProgress;
        }

        // GET: api/EntrustMissionProgressApi/user/my-tasks
        [HttpGet("user/my-tasks")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<EntrustMissionProgressDTO>>> GetMyTasks()
        {
            int userId = User.GetId();

            if (userId <= 0)
            {
                return Unauthorized(new ResultDTO { Ok = false, Code = 401, Message = "無效的使用者身份" });
            }

            var tasks = await _entrustMissionProgressService.GetMyProgressAsync(userId);

            return Ok(tasks);
        }


    }
}
