using CarbonProject.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CarbonProject.Middleware
{
    public class RememberMeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RememberMeMiddleware> _logger;

        public RememberMeMiddleware(RequestDelegate next, ILogger<RememberMeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

            public async Task InvokeAsync(HttpContext context)
            {
            // 如果已經有 Session 登入，直接繼續
            if (context.Session.GetString("isLogin") == "true")
            {
                await _next(context);
                return;
            }

            // 檢查 cookie 中是否有 JWT token
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
                            context.Session.SetString("isLogin", "true");
                            context.Session.SetString("Username", username);
                            context.Session.SetString("Role", role);
                            context.Session.SetInt32("MemberId", memberId);
                        }
                    }
                    else
                    {
                        context.Response.Cookies.Delete("AuthToken");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JWT token validation failed");
                    context.Response.Cookies.Delete("AuthToken");
                }
            }

            await _next(context);
        }
    }

    public static class RememberMeMiddlewareExtensions
    {
        public static IApplicationBuilder UseRememberMe(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RememberMeMiddleware>();
        }
    }
}
