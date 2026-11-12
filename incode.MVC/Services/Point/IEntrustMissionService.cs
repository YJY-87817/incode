
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

        /// <summary>
        /// 處理已過截止日期的委託：沒有其他會員回覆的立即結案，有回覆的在截止後 7 天仍沒選最佳解答才結案；
        /// 結案時狀態改為 expired 並退還預扣點數。回傳本次結案的筆數
        /// </summary>
        Task<int> ExpireOverdueMissionsAsync(DateTime now);
    }
}
