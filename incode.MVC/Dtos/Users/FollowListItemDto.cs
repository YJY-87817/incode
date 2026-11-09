namespace incode.MVC.Dtos.Users
{
    /// <summary>追蹤清單項目（粉絲或追蹤中的人）；Id 為對方的會員編號。</summary>
    public class FollowListItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
