using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using incode2.Models;

namespace incode2.ViewModel
{
    public class AdminViewModel
    {
        [Display(Name = "ID")]       
        public int UserId { get; set; }

        [Display(Name = "email")]
        public string? Email { get; set; }

        [Display(Name = "名稱")]
        public string? Username { get; set; }

        [Display(Name = "電話")]
        public int? Phone { get; set; }

        [Display(Name = "登入歷史")]
        public DateTime? Logintime { get; set; }

        [Display(Name = "登入位置")]
        public string? Location { get; set; }

        [Display(Name = "是否啟用")]
        public bool? IsActive { get; set; }
    }
}
