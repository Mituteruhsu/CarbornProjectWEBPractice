using CarbonProject.Models.EFModels.RBAC;
using System.Collections.Generic;

namespace CarbonProject.Models.RBACViews
{
    public class PermissionsViewModel
    {
        public int PermissionId { get; set; }
        public string PermissionKey { get; set; }
        public string Description { get; set; }
        public List<int> CapabilityIds { get; set; } = new();
        public List<string> CapabilityNames { get; set; } = new();
        // 建構子：讓 Permission -> PermissionsViewModel
        public PermissionsViewModel(Permission p)
        {
            PermissionId = p.PermissionId;
            PermissionKey = p.PermissionKey;
            Description = p.Description;

            CapabilityIds = p.PermissionCapabilities?
                .Select(pc => pc.CapabilityId)
                .ToList() ?? new List<int>();

            CapabilityNames = p.PermissionCapabilities?
                .Select(pc => pc.Capability.Name)
                .ToList() ?? new List<string>();
        }

        // 無參數建構子給 Razor / ModelBinding 用
        public PermissionsViewModel() { }
    }
}