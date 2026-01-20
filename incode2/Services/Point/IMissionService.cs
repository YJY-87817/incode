using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IMissionService
    {
        Task<IEnumerable<MissionDTO>> GetAllMissionsAsync();
        Task<MissionDTO> GetMissionByIdAsync(int id);
        Task<ResultDTO> CreateMissionAsync(MissionDTO dto);
        Task<ResultDTO> UpdateMissionAsync(int id, MissionDTO dto);
        Task<ResultDTO> DeleteMissionAsync(int id);
    }
}
