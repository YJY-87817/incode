using System.ComponentModel.DataAnnotations;

namespace incode.MVC.Dtos.Users
{
    public class UserProfileStatsDto
    {
        public int ArticleCount { get; set; }    // 文章數
        public int MissionCount { get; set; }    // 任務數 (完成數)
        public int PointBalance { get; set; }    // 目前點數
        public int FollowingCount { get; set; }  // 關注中 (我追蹤別人)
        public int FollowerCount { get; set; }   // 粉絲數 (別人追蹤我)
    }


}
