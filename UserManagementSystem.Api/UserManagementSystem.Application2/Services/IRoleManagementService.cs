using UserManagementSystem.Application.DTOs;

namespace UserManagementSystem.Application.Services
{
    public interface IRoleManagementService
    {
        Task<ApiResponse<bool>> PromoteToManagerAsync(int adminId, int userId, int teamId);
        Task<ApiResponse<bool>> DemoteManagerAsync(int adminId, int managerId);
        Task<ApiResponse<bool>> ChangeUserRoleAsync(int managerId, int userId, string newRoleName);
        Task<ApiResponse<bool>> ValidateRoleChangeAsync(int requestingUserId, int targetUserId, string newRoleName);
        Task<ApiResponse<List<string>>> GetAvailableRolesForUserAsync(int requestingUserId, int targetUserId);
    }
}