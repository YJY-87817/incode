using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IMissionProgressService
    {
        Task<IEnumerable<MissionProgressDTO>> GetAllProgressAsync();
        Task<MissionProgressDTO> GetProgressByIdAsync(int id);
        Task<ResultDTO> CreateProgressAsync(MissionProgressDTO dto);
        Task<ResultDTO> UpdateProgressAsync(int id, MissionProgressDTO dto);
        Task<ResultDTO> DeleteProgressAsync(int id);
    }
}
