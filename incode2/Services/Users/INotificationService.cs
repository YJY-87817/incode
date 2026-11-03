
namespace incode2.Services.Users
{
    public interface INotificationService
    {
        Task<List<UserNotification>> GetNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int userId, int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }
}