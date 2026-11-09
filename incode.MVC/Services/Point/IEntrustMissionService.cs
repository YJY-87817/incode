
namespace incode.MVC.Services.Point
{
    public interface IEntrustMissionService
    {
        Task<IEnumerable<EntrustMissionDTO>> GetAllEntrustMissionAsync(); //讀取全部委託任務
        Task<EntrustMissionDTO> GetEntrustMissionByIdAsync(int id);
        Task<IEnumerable<EntrustMissionDTO>> GetMyEntrustMissionsAsync(int userId);
        Task<ResultDTO> CreateEntrustMissionAsync(EntrustMissionDTO dto);
        Task<ResultDTO> CancelEntrustMissionAsync(int entrustMissionId, int userId);

        /// <summary>後台取消委託（不限發起人、已有回覆也可以），退還預扣點數</summary>
        Task<ResultDTO> CancelEntrustMissionByAdminAsync(int entrustMissionId);
    }
}
