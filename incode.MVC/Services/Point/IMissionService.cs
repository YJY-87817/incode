
namespace incode.MVC.Services.Point
{
    public interface IMissionService
    {
        Task<IEnumerable<MissionDTO>> GetAllMissionAsync(); //讀取全部平台任務
        Task<ResultDTO> ClaimMissionAsync(int missionId, int userId);//接取任務
        Task<MissionDTO> GetMissionByIdAsync(int id);
        Task<ResultDTO> CreateMissionAsync(MissionDTO dto);
        Task TriggerMissionAsync(int userId, string actionType, int increment = 1);

        /// <summary>
        /// 以實際資料重新計算進度（例如按讚任務：本週期內按讚的不同內容數）。
        /// countInCycle 收到該任務本週期的起訖時間，回傳目前的數量；進度只增不減
        /// </summary>
        Task SyncMissionCountAsync(int userId, string actionType, Func<DateTime, DateTime, Task<int>> countInCycle);
    }
}
