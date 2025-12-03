using CarbonProject.Attributes;
using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Models.RBACViews;
using CarbonProject.Repositories;
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
        private readonly RbacRepository _rbacRepo;
        private readonly RoleService _roleService;
        private readonly PermissionService _permissionService;
        private readonly CapabilityService _capabilityService;

        public RbacController(RbacDbContext context, RBACService rbacservice, RoleService roleService, PermissionService permissionService, CapabilityService capabilityService, RbacRepository rbacRepository)
        {
            _context = context;
            _rbacService = rbacservice;
            _rbacRepo = rbacRepository;
            _roleService = roleService;
            _permissionService = permissionService;
            _capabilityService = capabilityService;
        }
        // 使用 RoleService 範例
        // From -> Service/RoleService.cs
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleService.GetRolesWithPermissionsAsync();
            var permissions = await _permissionService.GetCapabilitiesWithPermissionsAsync();
            var capabilities = await _capabilityService.GetAllCapabilitiesAsync();

            var viewModel = new RbacIndexViewModel
            {
                // 直接使用 service 回傳的 ViewModel
                Roles = roles ?? new List<RolesViewModel>(),

                Permissions = permissions?? new List<PermissionsViewModel>(),

                Capabilities = capabilities?.Select(c => new CapabilitiesViewModel(c)).ToList()
                        ?? new List<CapabilitiesViewModel>()
            };

            return View(viewModel);
        }
        // ===== Roles CRUD =====
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(new RolesViewModel());
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RolesViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _roleService.CreateRoleAsync(model.RoleName, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> EditRole(int roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role == null) return NotFound();

            var vm = new RolesViewModel
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList()
            };
            return View(vm);
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RolesViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _roleService.UpdateRoleAsync(model.RoleId, model.RoleName, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            await _roleService.DeleteRoleAsync(roleId);
            return RedirectToAction("Index");
        }

        // Role -> Permissions 授權
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> RolePermissions(int roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role == null) return NotFound();

            ViewBag.AllPermissions = await _permissionService.GetAllPermissionsAsync();
            return View(role);
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPermissionsToRole(int roleId, int[] permissionIds)
        {
            var currentPermissions = await _permissionService.GetPermissionsByRoleAsync(roleId);

            // 移除舊權限
            foreach (var p in currentPermissions)
            {
                if (!permissionIds.Contains(p.PermissionId))
                    await _permissionService.RemovePermissionFromRoleAsync(p.PermissionId, roleId);
            }

            // 新增權限
            foreach (var pid in permissionIds)
            {
                if (!currentPermissions.Any(p => p.PermissionId == pid))
                    await _permissionService.AssignPermissionToRoleAsync(pid, roleId);
            }

            return RedirectToAction("Index");
        }
        // ===== Permission CRUD =====
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public IActionResult CreatePermission()
        {
            return View(new PermissionsViewModel());
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePermission(PermissionsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _permissionService.CreatePermissionAsync(model.PermissionKey, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> EditPermission(int permissionId)
        {
            var perm = await _permissionService.GetPermissionByIdAsync(permissionId);
            if (perm == null) return NotFound();

            var vm = new PermissionsViewModel
            {
                PermissionId = perm.PermissionId,
                PermissionKey = perm.PermissionKey,
                Description = perm.Description,
                CapabilityIds = perm.PermissionCapabilities.Select(pc => pc.CapabilityId).ToList()
            };
            return View(vm);
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPermission(PermissionsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _permissionService.UpdatePermissionAsync(model.PermissionId, model.PermissionKey, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> DeletePermission(int permissionId)
        {
            await _permissionService.DeletePermissionAsync(permissionId);
            return RedirectToAction("Index");
        }

        // Permission -> Capabilities 授權
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> PermissionCapabilities(int permissionId)
        {
            var perm = await _permissionService.GetPermissionByIdAsync(permissionId);
            if (perm == null) return NotFound();

            ViewBag.AllCapabilities = await _capabilityService.GetAllCapabilitiesAsync();
            return View(perm);
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCapabilitiesToPermission(int permissionId, int[] capabilityIds)
        {
            var currentCaps = await _capabilityService.GetCapabilitiesByPermissionAsync(permissionId);

            // 移除舊關聯
            foreach (var c in currentCaps)
            {
                if (!capabilityIds.Contains(c.CapabilityId))
                    await _capabilityService.RemoveCapabilityFromPermissionAsync(c.CapabilityId, permissionId);
            }

            // 新增關聯
            foreach (var cid in capabilityIds)
            {
                if (!currentCaps.Any(c => c.CapabilityId == cid))
                    await _capabilityService.AssignCapabilityToPermissionAsync(cid, permissionId);
            }

            return RedirectToAction("Index");
        }
        // ===== Capability CRUD =====
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public IActionResult CreateCapability()
        {
            return View(new CapabilitiesViewModel());
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCapability(CapabilitiesViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _capabilityService.CreateCapabilityAsync(model.Name, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> EditCapability(int capabilityId)
        {
            var cap = await _capabilityService.GetCapabilityByIdAsync(capabilityId);
            if (cap == null) return NotFound();

            var vm = new CapabilitiesViewModel
            {
                CapabilityId = cap.CapabilityId,
                Name = cap.Name,
                Description = cap.Description
            };
            return View(vm);
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCapability(CapabilitiesViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _capabilityService.UpdateCapabilityAsync(model.CapabilityId, model.Name, model.Description);
            return RedirectToAction("Index");
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        [HttpGet]
        public async Task<IActionResult> DeleteCapability(int capabilityId)
        {
            await _capabilityService.DeleteCapabilityAsync(capabilityId);
            return RedirectToAction("Index");
        }

        //// 使用 PermissionService 範例
        //// From -> Service/PermissionService.cs
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        //public async Task<IActionResult> Permissions()
        //{
        //    var permissions = await _permissionService.GetAllPermissionsAsync();
        //    return View(permissions);
        //}
        //// 使用 CapabilityService 範例
        //// From -> Service/CapabilityService.cs
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        //public async Task<IActionResult> Capabilities()
        //{
        //    var caps = await _capabilityService.GetAllCapabilitiesAsync();
        //    return View(caps);
        //}
        //// 顯示所有角色
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        //public async Task<IActionResult> Roles()
        //{
        //    var roles = await _context.Roles.ToListAsync();
        //    return View(roles);
        //}

        //// 顯示角色對應權限
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        //public async Task<IActionResult> RolePermissions(int roleId = 1)
        //{
        //    // 查找 Role 並 Include 其對應的 Permissions
        //    var role = await _context.Roles
        //        .Include(r => r.RolePermissions)
        //        .ThenInclude(rp => rp.Permission)
        //        .FirstOrDefaultAsync(r => r.RoleId == roleId);

        //    Debug.WriteLine("===== Controllers/RbacController.cs =====");
        //    Debug.WriteLine("--- RolePermissions ---");
        //    Debug.WriteLine($"RoleName: {role.RoleName}");
        //    foreach (var rp in role.RolePermissions)
        //    {
        //        Debug.WriteLine($"RolePermissions here:{rp.Permission?.PermissionKey} - {rp.Permission?.Description}");
        //    }

        //    if (role == null) return NotFound();

        //    return View(role); // 傳 Role 給 View
        //}

        //// 顯示所有使用者
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        //public async Task<IActionResult> Users()
        //{
        //    var users = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ToListAsync();
        //    return View(users);
        //}
        //// 啟用中的使用者，沒有被停權、沒有被刪除、允許登入系統。
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]

        //public async Task<IActionResult> ActiveUsers()
        //{
        //    var activeUsers = await _rbacService.GetActiveUsers();
        //    return View(activeUsers); // 傳給 ActiveUsers.cshtml 顯示
        //}
    }
}