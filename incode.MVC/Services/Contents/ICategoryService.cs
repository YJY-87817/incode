namespace incode.MVC.Services.Contents
{
    public interface ICategoryService
    {
        /// <summary>所有分類與統計（主題數 = 主文；文章數 = 主文 + 留言，不含 IsActive = 3 已刪除的內容）。</summary>
        Task<List<CategoryDto>> GetCategoriesAsync();

        /// <summary>新增分類；指定的父分類不存在時回傳 null。</summary>
        Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto request);
    }
}
