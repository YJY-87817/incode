namespace incode.MVC.Services.Point
{
    /// <summary>
    /// 後台平台任務（Mission）與任務進度（MissionProgress，唯讀）。
    /// 任務進度只由伺服器端觸發（領獎、一次性任務防重複都依賴進度紀錄），後台只能查詢。
    /// 參數不合法或不能執行時丟 BusinessException。
    /// </summary>
    public interface IAdminMissionService
    {
        Task<List<Mission>> GetMissionsAsync();

        Task<Mission?> GetMissionAsync(int missionId);

        Task<Mission> CreateMissionAsync(Mission form);

        /// <summary>找不到回傳 false</summary>
        Task<bool> UpdateMissionAsync(int missionId, Mission form);

        /// <summary>已有會員進度的任務不能刪除（保留領獎紀錄），請改為停用</summary>
        Task DeleteMissionAsync(int missionId);

        /// <summary>依任務或會員篩選（兩者都有時以任務為準），並回傳頁面標題</summary>
        Task<(List<MissionProgress> Items, string Title)> GetProgressAsync(int? missionId, int? userId);

        Task<MissionProgress?> GetProgressByIdAsync(int missionProgressId);
    }
}
