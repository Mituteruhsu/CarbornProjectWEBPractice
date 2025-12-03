using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Service.RBAC
{
    public class RbacRepository
    {
        private readonly RbacDbContext _context;

        public RbacRepository(RbacDbContext context)
        {
            _context = context;
        }

        // Roles
        public async Task<List<Role>> GetRolesAsync() => await _context.Roles.ToListAsync();
        public async Task<Role> GetRoleByIdAsync(int id) => await _context.Roles.FindAsync(id);
        public async Task AddRoleAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRoleAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRoleAsync(Role role)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }

        // Permissions
        public async Task<List<Permission>> GetPermissionsAsync() => await _context.Permissions.ToListAsync();
        public async Task<Permission> GetPermissionByIdAsync(int id) => await _context.Permissions.FindAsync(id);

        // Capabilities
        public async Task<List<Capability>> GetCapabilitiesAsync() => await _context.Capabilities.ToListAsync();

        // RolePermissions
        public async Task<List<int>> GetPermissionIdsByRoleIdAsync(int roleId) =>
            await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

        public async Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            var existing = _context.RolePermissions.Where(rp => rp.RoleId == roleId);
            _context.RolePermissions.RemoveRange(existing);

            foreach (var pid in permissionIds)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
            }
            await _context.SaveChangesAsync();
        }

        // PermissionCapabilities
        public async Task<List<int>> GetCapabilityIdsByPermissionIdAsync(int permissionId) =>
            await _context.PermissionCapabilities
                .Where(pc => pc.PermissionId == permissionId)
                .Select(pc => pc.CapabilityId)
                .ToListAsync();

        public async Task UpdatePermissionCapabilitiesAsync(int permissionId, List<int> capabilityIds)
        {
            var existing = _context.PermissionCapabilities.Where(pc => pc.PermissionId == permissionId);
            _context.PermissionCapabilities.RemoveRange(existing);

            foreach (var cid in capabilityIds)
            {
                _context.PermissionCapabilities.Add(new PermissionCapability
                {
                    PermissionId = permissionId,
                    CapabilityId = cid
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
