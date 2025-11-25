using CarbonProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;

namespace CarbonProject.Middleware
{
    // Middleware：RBACMiddleware.cs
    // 用途：
    // - 自動檢查 Session，如果沒有 Session 但有 JWT Cookie，自動補 Session
    // - 可擴展：支援動態 RBAC 檢查（後續可加檢查權限清單）
    public class RBACMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RBACMiddleware> _logger;

        public RBACMiddleware(RequestDelegate next, ILogger<RBACMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. 如果已經有 Session 登入，直接繼續
            if (context.Session.GetString("isLogin") == "true")
            {
                await _next(context);
                return;
            }

            // 2. 檢查 Cookie 中的 JWT Token
            var jwtToken = context.Request.Cookies["AuthToken"];
            Debug.WriteLine($"嘗試從 Cookie 取 JWT : {jwtToken}");

            // 3. 如果 Cookie 沒有，檢查 Authorization header (Bearer)
            if (string.IsNullOrEmpty(jwtToken))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    jwtToken = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (!string.IsNullOrEmpty(jwtToken))
            {
                try
                {
                    var jwtService = context.RequestServices.GetRequiredService<JWTService>();
                    var principal = jwtService.ValidateToken(jwtToken);

                    if (principal != null)
                    {
                        var username = JWTService.GetUsername(principal);
                        var memberId = JWTService.GetMemberId(principal);
                        // 從 JWT 取 Roles / Permissions / Capabilities
                        var roles = JWTService.GetRoles(principal);
                        var permissions = JWTService.GetPermissions(principal);
                        var capabilities = JWTService.GetCapabilities(principal);

                        if (!string.IsNullOrEmpty(username) && roles.Any())
                        {
                            // 補 Session
                            context.Session.SetString("isLogin", "true");
                            context.Session.SetString("Username", username);
                            context.Session.SetInt32("MemberId", memberId);
                            context.Session.SetString("Roles", string.Join(",", roles));
                            context.Session.SetString("Permissions", string.Join(",", permissions));
                            context.Session.SetString("Capabilities", string.Join(",", capabilities));

                            _logger.LogDebug("[RBACMiddleware] Session restored from JWT for user {username}", username);
                        }
                        else
                        {
                            // 權限或 username 不足 -> 清除 cookie，避免無限嘗試
                            context.Response.Cookies.Delete("AuthToken");
                            context.Session.Clear();
                        }
                    }
                    else
                    {
                        // JWT 無效 → 清除 Cookie
                        context.Response.Cookies.Delete("AuthToken");
                        context.Session.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JWT token validation failed");
                    context.Response.Cookies.Delete("AuthToken");
                    context.Session.Clear();
                }
            }
            // 進入下一個 Middleware
            await _next(context);
        }
    }

    public static class RBACMiddlewareExtensions
    {
        public static IApplicationBuilder UseRBAC(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RBACMiddleware>();
        }
    }
}
