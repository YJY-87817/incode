using System;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace incode2.Partials
{
    public class UserMetadata
    {    
            [Display(Name = "ID")]
            [Required]
        public int UserId { get; set; }

        [Display(Name = "帳號")]
        [Required]
        public string Account { get; set; }

        [Display(Name = "暱稱")]
           
            public string? Username { get; set; }

         [Display(Name = "密碼")] //存雜湊
        [Required]
        public string Password { get; set; }

            [Display(Name = "電話")]
            public int? Phone { get; set; }

        [Display(Name = "大頭照")]
        public string? ImageUrl { get; set; }

        [Display(Name = "加入時間")]
            public DateTime? CreatedAt { get; set; }

        [Display(Name = "登入歷史")]
        public DateTime? Logintime { get; set; }

        [Display(Name = "是否啟用")]
        public bool? IsActive { get; set; }
    }
}
