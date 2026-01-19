using incode2.DTO;

namespace incode2.Services
{
    public interface IEntrustMissionService
    {
        Task<IEnumerable<EntrustMissionDTO>> GetAllAsync();
        Task<EntrustMissionDTO> GetByIdAsync(int id);
        Task<ResultDTO> CreateAsync(EntrustMissionDTO dto);
        Task<ResultDTO> UpdateAsync(int id, EntrustMissionDTO dto);
        Task<ResultDTO> DeleteAsync(int id);
    }
}
