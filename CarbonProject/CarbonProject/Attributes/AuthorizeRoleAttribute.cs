using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using CarbonProject.Service.Logging;
using CarbonProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MySqlX.XDevAPI;
using System.Diagnostics;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CarbonProject.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;
        private readonly string[] _permissions;
        private readonly string[] _capabilities;

        public AuthorizeRoleAttribute(string[] roles = null, string[] capabilities = null, string[] permissions = null)
        {
            _roles = roles ?? Array.Empty<string>();
            _permissions = permissions ?? Array.Empty<string>();
            _capabilities = capabilities ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            Debug.WriteLine("===== Attributes/AuthorizeRoleAttribute.cs =====");
            Debug.WriteLine("--- OnAuthorization ---");
            Debug.WriteLine($"開始角色驗證");

            // 1️ 檢查 Session 登入狀態
            var httpContext = context.HttpContext;
            var session = httpContext.Session;                       
            var isLogin = session.GetString("isLogin");
            var username = session.GetString("Username");
            var roles = session.GetString("Roles")?.Split(',') ?? Array.Empty<string>();
            var capabilities = session.GetString("Capabilities")?.Split(',') ?? Array.Empty<string>();
            var permissions = session.GetString("Permissions")?.Split(',') ?? Array.Empty<string>();

            Debug.WriteLine("--- 檢查 Session 登入狀態 ---");
            Debug.WriteLine($"session : {session}");
            Debug.WriteLine($"isLogin : {isLogin}");
            Debug.WriteLine($"username : {username}");
            Debug.WriteLine($"Session Roles : {string.Join(",", roles)}");
            Debug.WriteLine($"Session Capabilities : {string.Join(",", capabilities)}");
            Debug.WriteLine($"Session Permissions : {string.Join(",", permissions)}");
            var sessionData = session.Keys.ToDictionary(
                key => key,
                key => session.GetString(key)
            );
            var sessionData_json = System.Text.Json.JsonSerializer.Serialize(sessionData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Debug.WriteLine("___ Session 全部內容 ___");
            Debug.WriteLine(sessionData_json);

            if (isLogin != "true" || string.IsNullOrEmpty(username))
            {
                var authHeader = httpContext.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    string token = authHeader.Substring("Bearer ".Length).Trim();
                    // 2️ 從 DI 拿 JWTService
                    var jwtService = httpContext.RequestServices.GetService(typeof(JWTService)) as JWTService;

                    if (jwtService != null)
                    {
                        var principal = jwtService.ValidateToken(token);
                        if (principal != null)
                        {
                            username = JWTService.GetUsername(principal);

                            // 將 JWT 資訊寫入 Session
                            session.SetString("isLogin", "true");
                            session.SetString("Username", username);
                            session.SetString("Roles", string.Join(",", JWTService.GetRoles(principal)));
                            session.SetString("Permissions", string.Join(",", JWTService.GetPermissions(principal)));
                            session.SetString("Capabilities", string.Join(",", JWTService.GetCapabilities(principal)));

                            isLogin = "true";
                        }
                    }
                }
            }

            if (isLogin != "true" || string.IsNullOrEmpty(username))
            {
                Debug.WriteLine("驗證錯誤-回到 Login 頁面");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 2️ 取得 DbContext 與 ActivityLogService
            var db = httpContext.RequestServices.GetService(typeof(RbacDbContext)) as RbacDbContext;
            var logger = httpContext.RequestServices.GetService(typeof(ActivityLogService)) as ActivityLogService;
            Debug.WriteLine("--- 2 取得 DbContext 與 ActivityLogService ---");
            Debug.WriteLine($"db : {db}");
            Debug.WriteLine($"logger : {logger}");
            if (db == null || logger == null)
            {
                Debug.WriteLine("db || logger 錯誤-回到 Login 頁面");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 3️-1 取得使用者角色與功能
            var member = db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                                .ThenInclude(p => p.PermissionCapabilities)
                                    .ThenInclude(pc => pc.Capability)
                .Include(u => u.UserCompanyRoles)
                    .ThenInclude(ucr => ucr.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                                .ThenInclude(p => p.PermissionCapabilities)
                .FirstOrDefault(u => u.Username == username);

            Debug.WriteLine("--- 取得使用者角色與功能 ---");
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            Debug.WriteLine("以下 options 先隱藏");
            Debug.WriteLine($"使用者角色與功能:{options}");
            var member_json = System.Text.Json.JsonSerializer.Serialize(member, options);
            Debug.WriteLine("以下 member_json 先隱藏");
            //Debug.WriteLine(member_json);

            if (member == null)
            {
                Debug.WriteLine("取得 Users 錯誤-回到 Login 頁面");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 3-2 檢查 Role
            bool roleMatch = _roles.Length == 0 || member.UserRoles.Any(ur => _roles.Contains(ur.Role.RoleName)) ||
                             member.UserCompanyRoles.Any(ucr => _roles.Contains(ucr.Role.RoleName));
            Debug.WriteLine("--- 檢查 Role ---");
            Debug.WriteLine($"roleMatch : {roleMatch}");

            // 4-1 取得使用者 Capability
            var userCapabilities = member.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .SelectMany(rp => rp.Permission.PermissionCapabilities)
                .Where(pc => pc.Capability != null)
                .Select(pc => pc.Capability.Name)
                .Distinct()
                .ToList();
            foreach (var ur in member.UserRoles)
            {
                Debug.WriteLine($"=== Role: {ur.Role?.RoleName} ===");
                if (ur.Role?.RolePermissions != null)
                {
                    Debug.WriteLine($"RolePermissions Count: {ur.Role.RolePermissions.Count}");
                    foreach (var rp in ur.Role.RolePermissions)
                    {
                        Debug.WriteLine($"  -> PermissionId: {rp.PermissionId}");
                    }
                }
                else
                {
                    Debug.WriteLine("RolePermissions is NULL");
                }
            }
            Debug.WriteLine("--- 取得使用者 Capability ---");
            var userCapabilities_json = System.Text.Json.JsonSerializer.Serialize(userCapabilities, options);
            Debug.WriteLine(userCapabilities_json);

            // 4-2 將 Permissions 轉成 Capability
            var requiredCapabilities = db.Capabilities
                .Where(c => c.PermissionCapabilities
                    .Any(pc => _permissions.Contains(pc.Permission.PermissionKey)))
                .Select(c => c.Name)
                .Distinct()
                .ToList();

            Debug.WriteLine("--- Permissions 轉成 Capability ---");
            foreach (var cap in requiredCapabilities)
            {
                Debug.WriteLine($"Capability: {cap}");
            }

            // 4-3 將 Attribute 指定的 capabilities 也加入比對
            requiredCapabilities.AddRange(_capabilities);
            Debug.WriteLine("--- Attribute 指定的 capabilities 也加入比對 ---");
            var requiredPermissionCapabilities_json = System.Text.Json.JsonSerializer.Serialize(requiredCapabilities, options);
            Debug.WriteLine(requiredPermissionCapabilities_json);

            // 4-4 檢查 Capability
            bool capabilityMatch = requiredCapabilities.Count == 0
                                    || requiredCapabilities.Any(c => userCapabilities.Contains(c));

            Debug.WriteLine("--- 檢查 Capability ---");
            Debug.WriteLine(requiredCapabilities.Count);
            Debug.WriteLine(requiredCapabilities.Any(c => userCapabilities.Contains(c)));
            Debug.WriteLine($"capabilityMatch : {capabilityMatch}");

            // 5 不符合授權 → Redirect Login
            if (!roleMatch || !capabilityMatch)
            {
                // 記錄 ActivityLog
                Debug.WriteLine("--- Role/Capability 不符合授權-記錄 ActivityLog ---");
                logger.LogAsync(
                    memberId: member.MemberId,
                    companyId: member.CompanyId,
                    actionType: "Auth.AccessDenied",
                    actionCategory: "Auth",
                    outcome: "Denied",
                    ip: httpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
                    createdBy: username,
                    detailsObj: new { roles = _roles, capabilities = _capabilities }
                ).Wait();

                Debug.WriteLine("記錄 ActivityLog 完成-回到 Login 頁面");
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 若都符合，放行 (nothing to do)
            Debug.WriteLine("[AuthorizeRole] Authorization passed.");
        }
    }
}

// 1️ 單純用 Role
//[AuthorizeRole(roles: new[] { "Admin" })]

// 2️ 用單一 Capability
//[AuthorizeRole(capabilities: new[] { "View" })]

// 3️ 用 Permission（自動包含對應 CRUD Capability）
//[AuthorizeRole(permissions: new[] { "ManageUsers" })]

// 4️ 混合使用
//[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "ManageUsers" })]
