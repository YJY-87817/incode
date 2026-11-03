
namespace incode2.Services.Users
{
    public interface IStatusService
    {
        Task<UserStatus> GetUserStatusAsync(int userId);
    }
}
