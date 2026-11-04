
namespace incode.MVC.Services.Users
{
    public interface IStatusService
    {
        Task<UserStatus> GetUserStatusAsync(int userId);
    }
}
