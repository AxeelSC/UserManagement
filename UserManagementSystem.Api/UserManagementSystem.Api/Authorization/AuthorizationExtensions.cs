using System.Security.Claims;

namespace UserManagementSystem.Infrastructure.Authorization
{
    public static class AuthorizationExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        public static string GetUsername(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value ?? "";
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }

        public static int? GetTeamId(this ClaimsPrincipal user)
        {
            var teamIdClaim = user.FindFirst("teamId");
            return teamIdClaim != null && int.TryParse(teamIdClaim.Value, out var teamId) ? teamId : null;
        }

        public static List<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }

        public static bool IsInRole(this ClaimsPrincipal user, params string[] roles)
        {
            var userRoles = user.GetRoles();
            return roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }

        public static bool IsManager(this ClaimsPrincipal user)
        {
            return user.IsInRole("Manager");
        }
    }
}