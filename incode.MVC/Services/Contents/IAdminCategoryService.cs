namespace incode.MVC.Services.Contents
{
    /// <summary>
    /// 後台論壇分類。狀態（IsActive）用 ContentCategoryStatus：會員新增 / 官方 / 停用（停用的分類不能再發新主題）。
    /// 不合法（父分類不存在、形成循環、刪除時還有文章或子分類）丟 BusinessException。
    /// </summary>
    public interface IAdminCategoryService
    {
        /// <summary>DataTables 用的分類清單</summary>
        Task<List<AdminCategoryRow>> GetRowsAsync();

        Task<ContentCategory?> GetAsync(int contentCategoryId);

        /// <summary>父分類下拉選單（編輯時排除自己與自己的子孫）</summary>
        Task<List<(int Id, string Name)>> GetParentOptionsAsync(int? editingId = null);

        Task<ContentCategory> CreateAsync(ContentCategory form);

        /// <summary>找不到回傳 false</summary>
        Task<bool> UpdateAsync(int contentCategoryId, ContentCategory form);

        Task DeleteAsync(int contentCategoryId);
    }

    public record AdminCategoryRow(int ContentCategoryId, int? ParentCategoryId, string CategoryName, string? Description, int IsActive, string Status);
}
