using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Contents
{
    public class CreateCategoryDto
    {
        public int? ParentCategoryId { get; set; }

        // 與後台分類表單相同的規則（ContentCategoryStatus.NameMinLength～NameMaxLength）：原本會員可以建立 100 字的名稱，
        // 後台表單只允許 50 字，管理員就無法編輯這些分類（第三次全專案檢查 56）
        [Required]
        [StringLength(ContentCategoryStatus.NameMaxLength, MinimumLength = ContentCategoryStatus.NameMinLength, ErrorMessage = "分類名稱需為 2～50 字")]
        public string Name { get; set; } = "";

        [StringLength(ContentCategoryStatus.DescriptionMaxLength)]
        public string? Description { get; set; } // 選填 (前端未填時送 null；原本為必填導致新增分類失敗)
    }
}
