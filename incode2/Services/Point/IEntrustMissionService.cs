using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
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
