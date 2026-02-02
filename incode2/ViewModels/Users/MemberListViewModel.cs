using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace incode2.ViewModels
{
    public class MemberListViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "使用者")]
        public string? Username { get; set; }
        public string StatusText { get; set; } // 直接由後端組好 "異常 (IP變動)" 給前端顯示
        public string LastLoginTimeFormatted { get; set; } // 轉好格式的時間字串
    }
}