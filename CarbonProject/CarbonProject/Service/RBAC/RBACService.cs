using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Service.RBAC
{
    public class RBACService
    {
        private readonly RbacDbContext _context;

        public RBACService(RbacDbContext context)
        {
            _context = context;
        }

        // ===== 使用者查詢 =====
        // -- R-1 有啟用的使用者 --
        public async Task<List<User>> GetActiveUsers()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();
        }
        // -- R-2 ID 來查詢使用者 --
        public async Task<User?> GetUserByIdAsync(int memberId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                                .ThenInclude(p => p.PermissionCapabilities)
                                    .ThenInclude(pc => pc.Capability)
                .FirstOrDefaultAsync(u => u.MemberId == memberId);
        }
        // ===== 使用者登入 RBAC 聚合 =====
        // -- R-3 回傳角色列表、權限列表、功能點列表 --
        public async Task<(List<string> Roles, List<string> Permissions, List<string> Capabilities)> GetUserRBACAsync(int memberId)
        {
            var user = await GetUserByIdAsync(memberId);
            if (user == null) return (new List<string>(), new List<string>(), new List<string>());

            var roles = user.UserRoles
                .Select(ur => ur.Role.RoleName)
                .Distinct()
                .ToList();

            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionKey)
                .Distinct()
                .ToList();

            var capabilities = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .SelectMany(rp => rp.Permission.PermissionCapabilities)
                .Select(pc => pc.Capability.Name)
                .Distinct()
                .ToList();

            return (roles, permissions, capabilities);
        }
        // ===== 授權檢查 =====
        // -- Check-1 檢查使用者是否擁有特定 PermissionKey --
        public async Task<bool> UserHasPermissionAsync(int memberId, string permissionKey)
        {
            var user = await GetUserByIdAsync(memberId);
            if (user == null) return false;

            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.PermissionKey);

            return permissions.Contains(permissionKey);
        }

        // -- Check-2 檢查使用者是否擁有特定 Capability --
        public async Task<bool> UserHasCapabilityAsync(int memberId, string capabilityName)
        {
            var user = await GetUserByIdAsync(memberId);
            if (user == null) return false;

            var capabilities = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .SelectMany(rp => rp.Permission.PermissionCapabilities)
                .Select(pc => pc.Capability.Name);

            return capabilities.Contains(capabilityName);
        }

        // -- Check-3 可同時檢查 Permission + Capability --
        public async Task<bool> UserHasPermissionOrCapabilityAsync(int memberId, string? permissionKey = null, string? capabilityName = null)
        {
            if (permissionKey != null && await UserHasPermissionAsync(memberId, permissionKey))
                return true;

            if (capabilityName != null && await UserHasCapabilityAsync(memberId, capabilityName))
                return true;

            return false;
        }
    }
}
