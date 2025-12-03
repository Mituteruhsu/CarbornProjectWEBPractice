using CarbonProject.Models.EFModels.RBAC;
using System.Collections.Generic;

namespace CarbonProject.Models.RBACViews
{
    public class RolesViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        // 額外提供給 View 的資料
        public List<int> PermissionIds { get; set; } = new();
        public List<string> PermissionDescriptions { get; set; } = new();
        public RolesViewModel() { }     // 無參數建構子（EF / Razor Page 需要）
        // 加上建構子：可以直接用 Role 轉換成 ViewModel
        public RolesViewModel(Role r)
        {
            RoleId = r.RoleId;
            RoleName = r.RoleName;
            Description = r.Description;

            // 讀取 PermissionId
            PermissionIds = r.RolePermissions?
                .Select(rp => rp.PermissionId)
                .ToList() ?? new List<int>();

            // ★ 重點：讀取 Permission.Description
            PermissionDescriptions = r.RolePermissions?
                .Select(rp => rp.Permission?.Description ?? "")
                .ToList() ?? new List<string>();
        }
    }
}