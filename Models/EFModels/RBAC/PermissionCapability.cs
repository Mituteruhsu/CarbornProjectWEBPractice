using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    // 權限 ↔ 功能細項
    public class PermissionCapability
    {
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public int CapabilityId { get; set; }
        public Capability Capability { get; set; } = null!;
    }
}