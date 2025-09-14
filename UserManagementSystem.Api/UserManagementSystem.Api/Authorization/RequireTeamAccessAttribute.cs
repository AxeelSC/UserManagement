using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Interfaces.Repositories;

namespace UserManagementSystem.Infrastructure.Authorization
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireTeamAccessAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _userIdParameterName;

        public RequireTeamAccessAttribute(string userIdParameterName = "id")
        {
            _userIdParameterName = userIdParameterName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResult("Authentication required"));
                return;
            }

            // Get current user info from token
            var currentUserIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var currentUserRoles = context.HttpContext.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value).ToList();

            if (currentUserIdClaim == null)
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse<object>.ErrorResult("Invalid token"));
                return;
            }

            var currentUserId = int.Parse(currentUserIdClaim.Value);

            // Admins have access to everything
            if (currentUserRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                return; // Allow access
            }

            // Get the target user ID from route parameters
            if (!context.RouteData.Values.TryGetValue(_userIdParameterName, out var userIdValue) ||
                !int.TryParse(userIdValue?.ToString(), out var targetUserId))
            {
                context.Result = new BadRequestObjectResult(
                    ApiResponse<object>.ErrorResult("Invalid user ID"));
                return;
            }

            // Users can always access their own data
            if (currentUserId == targetUserId)
            {
                return; // Allow access to own data
            }

            // For managers, check if target user is in their team
            if (currentUserRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            {
                var unitOfWork = context.HttpContext.RequestServices
                    .GetRequiredService<IUnitOfWork>();

                var currentUser = await unitOfWork.Users.GetByIdAsync(currentUserId);
                var targetUser = await unitOfWork.Users.GetByIdAsync(targetUserId);

                if (currentUser?.TeamId != null &&
                    currentUser.TeamId == targetUser?.TeamId)
                {
                    return; // Manager can access users in their team
                }
            }

            // Access denied
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<RequireTeamAccessAttribute>>();
            logger.LogWarning("Team access denied for user {CurrentUserId} trying to access user {TargetUserId}",
                currentUserId, targetUserId);

            context.Result = new ObjectResult(
                ApiResponse<object>.ErrorResult("Access denied. You can only access users in your team."))
            {
                StatusCode = 403
            };
        }
    }
}