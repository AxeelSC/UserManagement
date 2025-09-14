using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using UserManagementSystem.Application.DTOs;

namespace UserManagementSystem.Infrastructure.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _requiredRoles;

        public RequireRoleAttribute(params string[] roles)
        {
            _requiredRoles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResult("Authentication required"));
                return;
            }

            // Get user roles from claims
            var userRoles = context.HttpContext.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // Check if user has any of the required roles
            var hasRequiredRole = _requiredRoles.Any(requiredRole =>
                userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));

            if (!hasRequiredRole)
            {
                var username = context.HttpContext.User.Identity?.Name ?? "Unknown";
                var requiredRolesString = string.Join(", ", _requiredRoles);
                var userRolesString = string.Join(", ", userRoles);

                // Log authorization failure
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<RequireRoleAttribute>>();
                logger.LogWarning("Authorization failed for user {Username}. Required roles: [{RequiredRoles}], User roles: [{UserRoles}]",
                    username, requiredRolesString, userRolesString);

                context.Result = new ObjectResult(
                    ApiResponse<object>.ErrorResult($"Access denied. Required role(s): {requiredRolesString}"))
                {
                    StatusCode = 403
                };
            }
        }
    }
}