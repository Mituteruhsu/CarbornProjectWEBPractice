using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    // 角色 ↔ 權限
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}