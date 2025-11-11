using CarbonProject.Models.EFModels.RBAC;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Data
{
    //Seed Data(初始角色、權限、使用者)
    public static class RbacDbInitializer
    {
        public static void Initialize(RbacDbContext context)
        {
            context.Database.Migrate();

            // === 1️ 建立角色 ===
            if (!context.Roles.Any())
            {
                var roles = new Role[]
                {
            new Role { RoleName = "Member", Description = "一般使用者" },
            new Role { RoleName = "Manager", Description = "公司主管" },
            new Role { RoleName = "Admin", Description = "系統管理員" }
                };
                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            // === 2️ 建立 Capability（功能模組） ===
            if (!context.Capabilities.Any())
            {
                var capabilities = new Capability[]
                {
            new Capability { Name = "Account Management", Description = "帳號管理模組" },
            new Capability { Name = "Company Dashboard", Description = "公司儀表板模組" },
            new Capability { Name = "Emission Tracking", Description = "碳排放追蹤模組" },
            new Capability { Name = "ESG Reports", Description = "ESG 報表模組" }
                };
                context.Capabilities.AddRange(capabilities);
                context.SaveChanges();
            }

            // === 3️ 建立權限 ===
            if (!context.Permissions.Any())
            {
                var permissions = new Permission[]
                {
            new Permission { PermissionKey = "ViewProfile", Description = "檢視個人資料" },
            new Permission { PermissionKey = "ManageUsers", Description = "管理使用者" },
            new Permission { PermissionKey = "AssignRoles", Description = "分配角色" }
                };
                context.Permissions.AddRange(permissions);
                context.SaveChanges();
            }

            // === 4️ 建立 Role-Permission 對應 ===
            if (!context.RolePermissions.Any())
            {
                var roles = context.Roles.ToList();
                var permissions = context.Permissions.ToList();

                context.RolePermissions.AddRange(new RolePermission[]
                {
            new RolePermission
            {
                RoleId = roles.Single(r => r.RoleName == "Member").RoleId,
                PermissionId = permissions.Single(p => p.PermissionKey == "ViewProfile").PermissionId
            },
            new RolePermission
            {
                RoleId = roles.Single(r => r.RoleName == "Manager").RoleId,
                PermissionId = permissions.Single(p => p.PermissionKey == "ManageUsers").PermissionId
            },
            new RolePermission
            {
                RoleId = roles.Single(r => r.RoleName == "Admin").RoleId,
                PermissionId = permissions.Single(p => p.PermissionKey == "AssignRoles").PermissionId
            },
                });
                context.SaveChanges();
            }

            // === 5️ 建立 Capability-Permission 對應 ===
            if (!context.PermissionCapabilities.Any())
            {
                var capabilities = context.Capabilities.ToList();
                var permissions = context.Permissions.ToList();

                context.PermissionCapabilities.AddRange(new PermissionCapability[]
                {
            new PermissionCapability
            {
                CapabilityId = capabilities.Single(c => c.Name == "Account Management").CapabilityId,
                PermissionId = permissions.Single(p => p.PermissionKey == "ManageUsers").PermissionId
            },
            new PermissionCapability
            {
                CapabilityId = capabilities.Single(c => c.Name == "ESG Reports").CapabilityId,
                PermissionId = permissions.Single(p => p.PermissionKey == "ViewProfile").PermissionId
            }
                });
                context.SaveChanges();
            }
        }
    }
}
