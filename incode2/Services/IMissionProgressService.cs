using incode2.DTO;

namespace incode2.Services
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
