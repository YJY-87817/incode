namespace incode.MVC.Services.Point
{
    /// <summary>
    /// 後台委託任務：列表、詳情、只改說明性欄位、刪除已結束的委託、代會員建立（走預扣流程）。
    /// 取消並退款用 IEntrustMissionService.CancelEntrustMissionByAdminAsync。
    /// </summary>
    public interface IAdminEntrustMissionService
    {
        Task<List<EntrustMission>> GetAllAsync();

        Task<EntrustMission?> GetAsync(int entrustMissionId);

        /// <summary>可以建立委託的主題文章：公開中、主題、目前沒有進行中的委託（value = ContentId）</summary>
        Task<List<(int ContentId, string Text)>> GetEligibleTopicsAsync();

        /// <summary>代文章作者建立委託並預扣點數。失敗（點數不足、文章不符）丟 BusinessException</summary>
        Task<int> CreateAsync(EntrustMissionCreateViewModel form);

        /// <summary>只改標題、詳情、類別、截止日期、緊急。找不到回傳 false；必填缺漏丟 BusinessException</summary>
        Task<bool> UpdateDetailsAsync(int entrustMissionId, EntrustMission form);

        /// <summary>徵求中的委託還有預扣點數，不能直接刪除（丟 BusinessException）</summary>
        Task DeleteAsync(int entrustMissionId);
    }
}
