using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Contents
{
    public class CreateCategoryDto
    {
        public int? ParentCategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(200)]
        public string? Description { get; set; } // 選填 (前端未填時送 null；原本為必填導致新增分類失敗)
    }
}
