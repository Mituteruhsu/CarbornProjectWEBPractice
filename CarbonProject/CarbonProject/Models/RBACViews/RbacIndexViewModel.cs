using CarbonProject.Models.RBACViews;

public class RbacIndexViewModel
{
    public List<RolesViewModel> Roles { get; set; }
    public List<PermissionsViewModel> Permissions { get; set; }
    public List<CapabilitiesViewModel> Capabilities { get; set; }
}
