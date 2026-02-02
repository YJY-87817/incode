using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IEntrustMissionService
    {
        Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync(); //讀取全部委託任務
        Task<ResultDTO> ClaimEntrustMissionAsync(int entrustMissionId , int userId);//接取任務
        Task<EntrustMissionDTO> GetEntrustMissionByIdAsync(int id);
        Task<ResultDTO> CreateEntrustMissionAsync(EntrustMissionDTO dto);
        Task<ResultDTO> UpdateEntrustMissionAsync(int id, EntrustMissionDTO dto);
        Task<ResultDTO> DeleteEntrustMissionAsync(int id);
    }
}
