using Microsoft.EntityFrameworkCore;

namespace incode2.Services.Users
{
    public class NotificationService : INotificationService
    {
        private readonly incodedatabaseContext _context;

        public NotificationService(incodedatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<UserNotification>> GetNotificationsAsync(int userId)
        {
            return await _context.UserNotification
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.UserNotification
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _context.UserNotification.FindAsync(notificationId);

            // 找不到或是這則通知不屬於該用戶
            if (notification == null || notification.UserId != userId)
            {
                return false;
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadList = await _context.UserNotification
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();

            if (unreadList.Any())
            {
                foreach (var item in unreadList)
                {
                    item.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
