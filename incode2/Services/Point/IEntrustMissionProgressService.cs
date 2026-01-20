using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IEntrustMissionProgressService
    {
        Task<IEnumerable<EntrustMissionProgressDTO>> GetAllProgressAsync();
        Task<EntrustMissionProgressDTO> GetProgressByIdAsync(int id);
        Task<ResultDTO> CreateProgressAsync(EntrustMissionProgressDTO dto);
        Task<ResultDTO> UpdateProgressAsync(int id, EntrustMissionProgressDTO dto);
        Task<ResultDTO> DeleteProgressAsync(int id);
    }
}
