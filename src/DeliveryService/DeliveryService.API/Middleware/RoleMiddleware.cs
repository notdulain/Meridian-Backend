using System.Security.Claims;
using DeliveryService.API.Models;

namespace DeliveryService.API.Middleware;

/// <summary>
/// Simple middleware that reads a `role` claim (or ClaimTypes.Role) from the
/// authenticated user and, if present, stores the corresponding <see cref="UserRole"/>
/// value in <see cref="HttpContext.Items" /> under the key "UserRole".
///
/// We do not perform any authorization checks here; this just makes the role
/// available to downstream components (controllers or future authorization
/// handlers) without repeated claim parsing.
/// </summary>
public class RoleMiddleware
{
    private readonly RequestDelegate _next;

    public RoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                            ?? context.User.FindFirst("role")?.Value;
            if (!string.IsNullOrEmpty(roleClaim) &&
                Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var parsed))
            {
                context.Items["UserRole"] = parsed;
            }
        }

        // continue down the pipeline
        await _next(context);
    }
}
