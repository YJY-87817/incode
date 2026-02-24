using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IMissionService
    {
        Task<IEnumerable<MissionDTO>> GetAllMissionAsync(); //讀取全部平台任務
        Task<ResultDTO> ClaimMissionAsync(int missionId, int userId);//接取任務
        Task<MissionDTO> GetMissionByIdAsync(int id);
        Task<ResultDTO> CreateMissionAsync(MissionDTO dto);
        Task TriggerMissionAsync(int userId, string actionType, int increment = 1);
    }
}
