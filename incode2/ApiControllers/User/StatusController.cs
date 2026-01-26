using incode2.Models;
using incode2.Extensions; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode2.ApiController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StatusController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public StatusController(incodedatabaseContext context)
        {
            _context = context;
        }
        //
        [HttpGet]
        public async Task<ActionResult<UserStatus>> GetMyStatus()
        {
            int userId = User.GetId(); 

            var status = await _context.UserStatus.FindAsync(userId);

            if (status == null)
            {
                return Ok(new UserStatus
                {
                    UserId = userId,
                    Experience = 0,
                    Rank = 1,
                    Points = 0
                });
            }

            return Ok(status);
        }
    }
}