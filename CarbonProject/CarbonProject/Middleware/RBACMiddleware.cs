using CarbonProject.Services;
using Microsoft.AspNetCore.Http;
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
            if (!string.IsNullOrEmpty(jwtToken))
            {
                try
                {
                    var jwtService = context.RequestServices.GetRequiredService<JWTService>();
                    var principal = jwtService.ValidateToken(jwtToken);

                    if (principal != null)
                    {
                        var username = JWTService.GetUsername(principal);
                        var role = JWTService.GetRole(principal);
                        var memberId = JWTService.GetMemberId(principal);

                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(role))
                        {
                            // 補 Session
                            context.Session.SetString("isLogin", "true");
                            context.Session.SetString("Username", username);
                            context.Session.SetString("Role", role);
                            context.Session.SetInt32("MemberId", memberId);
                        }
                    }
                    else
                    {
                        // JWT 無效 → 清除 Cookie
                        context.Response.Cookies.Delete("AuthToken");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JWT token validation failed");
                    context.Response.Cookies.Delete("AuthToken");
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
