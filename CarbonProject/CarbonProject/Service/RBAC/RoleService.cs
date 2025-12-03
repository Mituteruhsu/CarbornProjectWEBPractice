using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Models.RBACViews;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarbonProject.Service.RBAC
{
    public class RoleService
    {
        private readonly RbacDbContext _context;

        public RoleService(RbacDbContext context)
        {
            _context = context;
        }

        // ===== 角色 CRUD =====

        // --R-1 取得所有角色 --
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
                .ToListAsync();
        }
        public async Task<List<RolesViewModel>> GetRolesWithPermissionsAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync();
            return roles.Select(r => new RolesViewModel
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,

                PermissionIds = r.RolePermissions
                    .Select(rp => rp.PermissionId)
                    .ToList(),

                // 一定會讀到 Permission.Description
                PermissionDescriptions = r.RolePermissions
                    .Select(rp => rp.Permission.Description ?? "(無描述)")
                    .ToList()

            }).ToList();
        }

        // --R-2 ID 取得角色 --
        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
        }

        // --C 建立角色 --
        public async Task<Role> CreateRoleAsync(string roleName, string? description = null)
        {
            var role = new Role
            {
                RoleName = roleName,
                Description = description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        // --U 更新角色 --
        public async Task<bool> UpdateRoleAsync(int roleId, string newName, string? newDescription)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return false;

            role.RoleName = newName;
            role.Description = newDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        // --D 刪除角色 --
        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
                return false;

            // 避免關聯資料殘留
            _context.UserRoles.RemoveRange(role.UserRoles);
            _context.RolePermissions.RemoveRange(role.RolePermissions);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        // ===== 使用者與角色關聯 =====

        // -- C 將使用者加入角色 --
        public async Task<bool> AddUserToRoleAsync(int memberId, int roleId)
        {
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.MemberId == memberId && ur.RoleId == roleId);
            if (exists)
                return false;

            var userRole = new UserRole
            {
                MemberId = memberId,
                RoleId = roleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        // --D 將使用者移出角色 --
        public async Task<bool> RemoveUserFromRoleAsync(int memberId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.MemberId == memberId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        // --R-1 查詢角色下的所有使用者 --
        public async Task<List<User>> GetUsersByRoleAsync(int roleId)
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.User)
                .ToListAsync();
        }

        // --R-2 查詢使用者擁有的所有角色 --
        public async Task<List<Role>> GetRolesByUserAsync(int memberId)
        {
            return await _context.UserRoles
                .Where(ur => ur.MemberId == memberId)
                .Select(ur => ur.Role)
                .ToListAsync();
        }
    }
}
