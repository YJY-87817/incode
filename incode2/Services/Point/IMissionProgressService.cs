using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IMissionProgressService
    {
        Task<IEnumerable<MissionProgressDTO>> GetAllProgressAsync();
        Task<ResultDTO> ClaimRewardAsync(int userId, int missionId);
        Task<ResultDTO> IncrementProgressAsync(int userId, int missionId, int increment);
        Task<IEnumerable<UserMissionViewDTO>> GetUserMissionsAsync(int userId);
    }
}
