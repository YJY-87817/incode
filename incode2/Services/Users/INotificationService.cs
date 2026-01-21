using incode2.Models;

namespace incode2.Services.Notification
{
    public interface INotificationService
    {
        Task<List<UserNotification>> GetNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }
}