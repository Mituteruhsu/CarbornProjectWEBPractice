using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Models.RBACViews;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Service.RBAC
{
    public class PermissionService
    {
        private readonly RbacDbContext _context;

        public PermissionService(RbacDbContext context)
        {
            _context = context;
        }

        // ===== 權限 CRUD =====

        // --R-1 取得所有權限 --
        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .Include(p => p.RolePermissions)
                .ThenInclude(rp => rp.Role)
                .Include(p => p.PermissionCapabilities)
                .ThenInclude(pc => pc.Capability)
                .ToListAsync();
        }
        public async Task<List<Models.RBACViews.PermissionsViewModel>> GetCapabilitiesWithPermissionsAsync()
        {
            var permissions = await _context.Permissions
                .Include(p => p.RolePermissions)              // 可選，看你是否需要 Role info
                    .ThenInclude(rp => rp.Role)
                .Include(p => p.PermissionCapabilities)      // 必須 Include PermissionCapabilities
                    .ThenInclude(pc => pc.Capability)       // 必須 Include Capability
                .ToListAsync();

            return permissions.Select(p => new PermissionsViewModel(p)).ToList();
        }

        // -- R-2 ID 取得權限 --
        public async Task<Permission?> GetPermissionByIdAsync(int permissionId)
        {
            return await _context.Permissions
                .Include(p => p.RolePermissions)
                .ThenInclude(rp => rp.Role)
                .Include(p => p.PermissionCapabilities)
                .ThenInclude(pc => pc.Capability)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);
        }

        // -- C 建立權限 --
        public async Task<Permission> CreatePermissionAsync(string permissionKey, string? description = null)
        {
            var permission = new Permission
            {
                PermissionKey = permissionKey,
                Description = description
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        // --U 更新權限 --
        public async Task<bool> UpdatePermissionAsync(int permissionId, string newKey, string? newDescription)
        {
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
                return false;

            permission.PermissionKey = newKey;
            permission.Description = newDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        // -- D 刪除權限 --
        public async Task<bool> DeletePermissionAsync(int permissionId)
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .Include(p => p.PermissionCapabilities)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null)
                return false;

            // 避免關聯資料殘留
            _context.RolePermissions.RemoveRange(permission.RolePermissions);
            _context.PermissionCapabilities.RemoveRange(permission.PermissionCapabilities);
            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }

        // ===== 權限與角色關聯 =====

        // -- C 將權限綁定到角色 --
        public async Task<bool> AssignPermissionToRoleAsync(int permissionId, int roleId)
        {
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.PermissionId == permissionId && rp.RoleId == roleId);

            if (exists)
                return false;

            var rp = new RolePermission
            {
                PermissionId = permissionId,
                RoleId = roleId
            };

            _context.RolePermissions.Add(rp);
            await _context.SaveChangesAsync();
            return true;
        }

        // -- D 從角色移除權限 --
        public async Task<bool> RemovePermissionFromRoleAsync(int permissionId, int roleId)
        {
            var rp = await _context.RolePermissions
                .FirstOrDefaultAsync(r => r.PermissionId == permissionId && r.RoleId == roleId);

            if (rp == null)
                return false;

            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
            return true;
        }

        // -- R-1 查詢角色擁有的所有權限 --
        public async Task<List<Permission>> GetPermissionsByRoleAsync(int roleId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync();
        }

        // -- R-2 查詢權限所屬的所有角色 --
        public async Task<List<Role>> GetRolesByPermissionAsync(int permissionId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .Select(rp => rp.Role)
                .ToListAsync();
        }
    }
}
