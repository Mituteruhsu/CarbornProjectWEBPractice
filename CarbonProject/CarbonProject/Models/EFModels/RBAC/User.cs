using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    public class User
    {
        [Key]
        public int MemberId { get; set; }       // 對應 DB 的主鍵
        public string Username { get; set; }    // 帳號
        public string Email { get; set; }       // 電子郵件
        public string PasswordHash { get; set; }
        public string FullName { get; set; } 
        public int? CompanyId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Address { get; set; }
        public string? ProfileImage { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }
        public string MembershipLevel { get; set; }
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastLogoutAt { get; set; }
        public DateTime? LastFailedLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public Company? Company { get; set; }

        public ICollection<UserCompanyRole> UserCompanyRoles { get; set; } = new List<UserCompanyRole>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}