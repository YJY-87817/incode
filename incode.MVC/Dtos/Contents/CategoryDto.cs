namespace incode.MVC.Dtos.Contents
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int? ParentCategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        // 前端論壇用到的統計
        public int TopicCount { get; set; }
        public int PostCount { get; set; }
    }
}
