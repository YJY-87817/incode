namespace incode.MVC.Services.Contents
{
    public interface ICategoryService
    {
        /// <summary>前台可用的分類與統計（不含停用的分類與其子分類；主題數 = 主文、文章數 = 主文 + 回覆，只算公開中的內容）。</summary>
        Task<List<CategoryDto>> GetCategoriesAsync();

        /// <summary>新增分類；指定的父分類不存在或已停用（含在停用分類底下）時回傳 null。</summary>
        Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto request);
    }
}
