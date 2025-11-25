using CarbonProject.Attributes;
using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Service.RBAC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarbonProject.Controllers
{
    public class RbacController : Controller
    {
        private readonly RbacDbContext _context;
        private readonly RBACService _rbacService;
        private readonly RoleService _roleService;
        private readonly PermissionService _permissionService;
        private readonly CapabilityService _capabilityService;

        public RbacController(RbacDbContext context, RBACService rbacservice, RoleService roleService, PermissionService permissionService, CapabilityService capabilityService)
        {
            _context = context;
            _rbacService = rbacservice;
            _roleService = roleService;
            _permissionService = permissionService;
            _capabilityService = capabilityService;
        }
        // 使用 RoleService 範例
        // From -> Service/RoleService.cs
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return View(roles);
        }
        // 使用 PermissionService 範例
        // From -> Service/PermissionService.cs
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Permissions()
        {
            var permissions = await _permissionService.GetAllPermissionsAsync();
            return View(permissions);
        }
        // 使用 CapabilityService 範例
        // From -> Service/CapabilityService.cs
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Capabilities()
        {
            var caps = await _capabilityService.GetAllCapabilitiesAsync();
            return View(caps);
        }
        // 顯示所有角色
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Roles()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        // 顯示角色對應權限
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
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
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ToListAsync();
            return View(users);
        }
        // 啟用中的使用者，沒有被停權、沒有被刪除、允許登入系統。
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]

        public async Task<IActionResult> ActiveUsers()
        {
            var activeUsers = await _rbacService.GetActiveUsers();
            return View(activeUsers); // 傳給 ActiveUsers.cshtml 顯示
        }
    }
}