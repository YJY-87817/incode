using incode2.DTO;
using incode2.Dtos;

namespace incode2.Services.Point
{
    public interface IEntrustMissionProgressService
    {
        Task<IEnumerable<EntrustMissionProgressDTO>> GetAllProgressAsync();

        Task<IEnumerable<EntrustMissionProgressDTO>> GetMyProgressAsync(int userId);
    }
}
