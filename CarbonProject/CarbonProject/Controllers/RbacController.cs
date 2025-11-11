using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarbonProject.Controllers
{
    public class RbacController : Controller
    {
        private readonly RbacDbContext _context;
        private readonly RBACService _rbacService;

        public RbacController(RbacDbContext context, RBACService rbacservice)
        {
            _context = context;
            _rbacService = rbacservice;

        }

        // 顯示所有角色
        public async Task<IActionResult> Roles()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        // 顯示角色對應權限
        public async Task<IActionResult> RolePermissions(int roleId = 1)
        {
            // 查找 Role 並 Include 其對應的 Permissions
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            Debug.WriteLine("===== Controllers/RbacController.cs =====");
            Debug.WriteLine("--- RolePermissions ---");
            Debug.WriteLine($"RoleName: {role.RoleName}");
            foreach (var rp in role.RolePermissions)
            {
                Debug.WriteLine($"RolePermissions here:{rp.Permission?.PermissionKey} - {rp.Permission?.Description}");
            }

            if (role == null) return NotFound();

            return View(role); // 傳 Role 給 View
        }

        // 顯示所有使用者
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ToListAsync();
            return View(users);
        }
        // 啟用中的使用者，沒有被停權、沒有被刪除、允許登入系統。
        public async Task<IActionResult> ActiveUsers()
        {
            var activeUsers = await _rbacService.GetActiveUsers();
            return View(activeUsers); // 傳給 ActiveUsers.cshtml 顯示
        }
    }
}