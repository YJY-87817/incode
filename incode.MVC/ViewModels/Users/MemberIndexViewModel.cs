using System;
using System.ComponentModel.DataAnnotations;

namespace incode.MVC.ViewModels.Users
{
    public class MemberIndexViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? Username { get; set; }
        public string? Phone { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? RoleName { get; set; }
        public bool HasSuspiciousLogin { get; set; }
    }
}