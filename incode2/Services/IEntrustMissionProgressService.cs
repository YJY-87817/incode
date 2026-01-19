using incode2.DTO;

namespace incode2.Services
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
