using CarbonProject.Models.EFModels.RBAC;

namespace CarbonProject.Models.RBACViews
{
    public class CapabilitiesViewModel
    {
        public int CapabilityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        // 從 EF Model Capability 轉成 ViewModel
        public CapabilitiesViewModel(Capability c)
        {
            CapabilityId = c.CapabilityId;
            Name = c.Name;
            Description = c.Description;
        }

        // 空建構子（必要）
        public CapabilitiesViewModel() { }
    }
}