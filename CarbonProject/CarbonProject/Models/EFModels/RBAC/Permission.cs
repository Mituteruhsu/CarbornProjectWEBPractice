using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    // 權限
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        public string PermissionKey { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<PermissionCapability> PermissionCapabilities { get; set; } = new List<PermissionCapability>();
    }
}