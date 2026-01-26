using incode2.Models;

namespace incode2.Services.user
{
    public interface IStatusService
    {
        Task<UserStatus> GetUserStatusAsync(int userId);
    }
}
