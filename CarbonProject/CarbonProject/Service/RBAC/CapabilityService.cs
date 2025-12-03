using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Models.RBACViews;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Service.RBAC
{
    public class CapabilityService
    {
        private readonly RbacDbContext _context;

        public CapabilityService(RbacDbContext context)
        {
            _context = context;
        }

        // ===== 功能點 CRUD =====
        // -- R-1 取得所有功能點 --
        public async Task<List<Capability>> GetAllCapabilitiesAsync()
        {
            return await _context.Capabilities
                .Include(c => c.PermissionCapabilities)
                .ThenInclude(pc => pc.Permission)
                .ToListAsync();
        }
        public async Task<List<CapabilitiesViewModel>> GetAllCapabilitiesViewAsync()
        {
            var capabilities = await _context.Capabilities
                .Include(c => c.PermissionCapabilities)
                .ThenInclude(pc => pc.Permission)
                .ToListAsync();

            return capabilities.Select(c => new CapabilitiesViewModel(c)).ToList();
        }
        // -- R-2 ID 取得功能點 --
        public async Task<Capability?> GetCapabilityByIdAsync(int capabilityId)
        {
            return await _context.Capabilities
                .Include(c => c.PermissionCapabilities)
                .ThenInclude(pc => pc.Permission)
                .FirstOrDefaultAsync(c => c.CapabilityId == capabilityId);
        }

        // -- C 建立功能點 --
        public async Task<Capability> CreateCapabilityAsync(string name, string? description = null)
        {
            var capability = new Capability
            {
                Name = name,
                Description = description
            };

            _context.Capabilities.Add(capability);
            await _context.SaveChangesAsync();
            return capability;
        }

        // -- U 更新功能點 --
        public async Task<bool> UpdateCapabilityAsync(int capabilityId, string newName, string? newDescription)
        {
            var capability = await _context.Capabilities.FindAsync(capabilityId);
            if (capability == null)
                return false;

            capability.Name = newName;
            capability.Description = newDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        // -- D 刪除功能點 --
        public async Task<bool> DeleteCapabilityAsync(int capabilityId)
        {
            var capability = await _context.Capabilities
                .Include(c => c.PermissionCapabilities)
                .FirstOrDefaultAsync(c => c.CapabilityId == capabilityId);

            if (capability == null)
                return false;

            // 避免關聯資料殘留
            _context.PermissionCapabilities.RemoveRange(capability.PermissionCapabilities);
            _context.Capabilities.Remove(capability);
            await _context.SaveChangesAsync();
            return true;
        }
        // ===== 功能點與權限關聯 =====
        
        // -- C 將功能點綁定到權限 --
        public async Task<bool> AssignCapabilityToPermissionAsync(int capabilityId, int permissionId)
        {
            var exists = await _context.PermissionCapabilities
                .AnyAsync(pc => pc.CapabilityId == capabilityId && pc.PermissionId == permissionId);

            if (exists)
                return false;

            var pc = new PermissionCapability
            {
                CapabilityId = capabilityId,
                PermissionId = permissionId
            };

            _context.PermissionCapabilities.Add(pc);
            await _context.SaveChangesAsync();
            return true;
        }

        // -- D 從權限解除功能點 --
        public async Task<bool> RemoveCapabilityFromPermissionAsync(int capabilityId, int permissionId)
        {
            var pc = await _context.PermissionCapabilities
                .FirstOrDefaultAsync(x => x.CapabilityId == capabilityId && x.PermissionId == permissionId);

            if (pc == null)
                return false;

            _context.PermissionCapabilities.Remove(pc);
            await _context.SaveChangesAsync();
            return true;
        }

        // -- R-1 查詢權限下的所有功能點 --
        public async Task<List<Capability>> GetCapabilitiesByPermissionAsync(int permissionId)
        {
            return await _context.PermissionCapabilities
                .Where(pc => pc.PermissionId == permissionId)
                .Select(pc => pc.Capability)
                .ToListAsync();
        }

        // -- R-2 查詢功能點對應的所有權限 --
        public async Task<List<Permission>> GetPermissionsByCapabilityAsync(int capabilityId)
        {
            return await _context.PermissionCapabilities
                .Where(pc => pc.CapabilityId == capabilityId)
                .Select(pc => pc.Permission)
                .ToListAsync();
        }

        // ===== 檢查使用者是否有某個 Capability =====
        public async Task<bool> UserHasCapabilityAsync(int memberId, string capabilityName)
        {
            // 找出使用者角色
            var roles = await _context.UserRoles
                .Where(ur => ur.MemberId == memberId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!roles.Any())
                return false;

            // 找出 Capability 與對應 Permission
            var capability = await _context.Capabilities
                .Include(c => c.PermissionCapabilities)
                .ThenInclude(pc => pc.Permission)
                .FirstOrDefaultAsync(c => c.Name == capabilityName);

            if (capability == null)
                return false;

            // 找出所有有權限的 Role
            var permittedRoleIds = capability.PermissionCapabilities
                .SelectMany(pc => _context.RolePermissions
                    .Where(rp => rp.PermissionId == pc.PermissionId)
                    .Select(rp => rp.RoleId))
                .Distinct()
                .ToList();

            return roles.Any(rid => permittedRoleIds.Contains(rid));
        }
    }
}
