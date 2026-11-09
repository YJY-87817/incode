using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace incode.MVC.ApiControllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserNotification>>> GetNotifications()
            => await _notificationService.GetNotificationsAsync(User.GetId());

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
            => Ok(await _notificationService.GetUnreadCountAsync(User.GetId()));

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // 找不到或不是自己的通知一律 404（不透露別人的通知是否存在）
            if (!await _notificationService.MarkAsReadAsync(User.GetId(), id))
                return NotFound();

            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(User.GetId());
            return NoContent();
        }
    }
}
