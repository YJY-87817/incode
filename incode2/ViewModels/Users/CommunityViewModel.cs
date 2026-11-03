using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels.Users
{
    public class CommunityViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "使用者")]
        public string? Username { get; set; }

        [Display(Name = "追蹤帳號")]
        public int? FollowerId { get; set; }

        [Display(Name = "被誰追蹤")]
        public int? FolloweeId { get; set; }        

        public List<SocialLinkItem> SocialLinks { get; set; } = new List<SocialLinkItem>();
        public class SocialLinkItem
                {
                    public int FolloweeId { get; set; }
                    public string? Username { get; set; }
                }
     }

    
}