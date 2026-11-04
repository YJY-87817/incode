
namespace incode.MVC.Services.Point
{
    public interface IMissionProgressService
    {
        Task<IEnumerable<MissionProgressDTO>> GetAllProgressAsync();
        Task<ResultDTO> ClaimRewardAsync(int userId, int missionId);
        Task<ResultDTO> IncrementProgressAsync(int userId, int missionId, int increment);
        Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId);
    }
}
