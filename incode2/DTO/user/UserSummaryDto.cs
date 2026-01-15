namespace incode2.DTO.user
{
    public class UserSummaryDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? ImageUrl { get; set; }
        
        public bool IsFollowing { get; set; }// 告訴前端：當前登入者是否正在追蹤這個人
    }
}
