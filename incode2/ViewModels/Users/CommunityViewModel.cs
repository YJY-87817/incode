using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class CommunityViewModel
    {
        //再整理 加裝備
        public int UserId { get; set; }

        [Display(Name = "使用者")]
        public string? Username { get; set; }

        [Display(Name = "追蹤帳號")]
        public int? FollowerId { get; set; }

        [Display(Name = "被誰追蹤")]
        public int? FolloweeId { get; set; }

        [Display(Name = "經驗值")]
        public int? Experience { get; set; }

        [Display(Name = "等級")]
        public int? Rank { get; set; }

        public IEnumerable<CommunityViewModel>? SocialLinks { get; set; }
    }
}