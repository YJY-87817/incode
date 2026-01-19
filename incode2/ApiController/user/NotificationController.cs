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
    public class NotificationController : ControllerBase
    {
        private readonly incodedatabaseContext _context;

        public NotificationController(incodedatabaseContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserNotification>>> GetNotifications()
        {
            int userId = User.GetId();

            return await _context.UserNotification
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            int userId = User.GetId(); 
            int count = await _context.UserNotification
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
            return Ok(count);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = User.GetId(); 

            var notification = await _context.UserNotification.FindAsync(id);
            if (notification == null) return NotFound();

            if (notification.UserId != userId) return Forbid();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = User.GetId();
            var unreadList = await _context.UserNotification
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();

            if (unreadList.Any())
            {
                foreach (var item in unreadList) item.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }
    }
}