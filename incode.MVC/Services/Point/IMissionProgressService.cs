
namespace incode.MVC.Services.Point
{
    public interface IMissionProgressService
    {
        Task<ResultDTO> ClaimRewardAsync(int userId, int missionId);
        Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId);
    }
}
