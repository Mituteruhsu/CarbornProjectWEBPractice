using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserCompanyRole> UserCompanyRoles { get; set; } = new List<UserCompanyRole>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}