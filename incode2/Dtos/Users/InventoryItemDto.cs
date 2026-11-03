using System.ComponentModel.DataAnnotations;

namespace incode2.Dtos.Users
{
    public class InventoryItemDto
    {
        public int InventoryId { get; set; }
        public int ItemId { get; set; }
        // 這裡預留 ItemName, ImageUrl，之後如果有 Item 表可以 Join 進來
        // public string ItemName { get; set; } 
        public int Quantity { get; set; }
    }
    public class AddItemDto
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
