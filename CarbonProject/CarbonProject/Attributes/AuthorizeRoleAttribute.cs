using CarbonProject.Data;
using CarbonProject.Service.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarbonProject.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;
        private readonly string[] _capabilities;

        public AuthorizeRoleAttribute(string[] roles = null, string[] capabilities = null)
        {
            _roles = roles ?? Array.Empty<string>();
            _capabilities = capabilities ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // 1️ 檢查 Session 登入狀態
            var isLogin = httpContext.Session.GetString("isLogin");
            var username = httpContext.Session.GetString("Username");
            if (isLogin != "true" || string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 2️ 取得 DbContext 與 ActivityLogService
            var db = httpContext.RequestServices.GetService(typeof(RbacDbContext)) as RbacDbContext;
            var logger = httpContext.RequestServices.GetService(typeof(CarbonProject.Service.Logging.ActivityLogService)) as ActivityLogService;

            if (db == null || logger == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 3️ 取得使用者角色與功能
            var member = db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                                .ThenInclude(p => p.PermissionCapabilities)
                .Include(u => u.UserCompanyRoles)
                    .ThenInclude(ucr => ucr.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                                .ThenInclude(p => p.PermissionCapabilities)
                .FirstOrDefault(u => u.Username == username);

            if (member == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 4️ 檢查 Role
            bool roleMatch = _roles.Length == 0 || member.UserRoles.Any(ur => _roles.Contains(ur.Role.RoleName)) ||
                             member.UserCompanyRoles.Any(ucr => _roles.Contains(ucr.Role.RoleName));

            // 5️ 檢查 Capability
            var userCapabilities = member.UserRoles
                                        .SelectMany(ur => ur.Role.RolePermissions)
                                        .SelectMany(rp => rp.Permission.PermissionCapabilities)
                                        .Select(pc => pc.Capability.Name)
                                    .Union(member.UserCompanyRoles
                                        .SelectMany(ucr => ucr.Role.RolePermissions)
                                        .SelectMany(rp => rp.Permission.PermissionCapabilities)
                                        .Select(pc => pc.Capability.Name))
                                    .ToList();

            bool capabilityMatch = _capabilities.Length == 0 || _capabilities.Any(c => userCapabilities.Contains(c));

            // 6️ 不符合授權 → Redirect Login
            if (!roleMatch || !capabilityMatch)
            {
                // 記錄 ActivityLog
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

                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
        }
    }
}
