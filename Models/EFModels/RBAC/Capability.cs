using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels.RBAC
{
    // 功能細項
    public class Capability
    {
        [Key]
        public int CapabilityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<PermissionCapability> PermissionCapabilities { get; set; } = new List<PermissionCapability>();
    }

}