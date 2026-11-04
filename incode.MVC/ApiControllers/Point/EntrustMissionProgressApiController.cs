using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace incode.MVC.ApiControllers.Point
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

        // 原本的 GET api/EntrustMissionProgressApi 會匿名列出所有人的委託任務進度，已移除 (請用 user/my-tasks)

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
