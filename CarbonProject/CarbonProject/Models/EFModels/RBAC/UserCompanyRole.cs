using System;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CarbonProject.Models.EFModels.RBAC
{
    public class UserCompanyRole
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public User User { get; set; } = null!;
        public int CompanyId { get; set; }
        // 對應 Companies table (EFModels/Company.cs)
        public Company Company { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public bool IsPrimary { get; set; } = false;
        public string? Status { get; set; }
        public int? AssignedBy { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}