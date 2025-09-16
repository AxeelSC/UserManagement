using Microsoft.Extensions.Logging;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Infrastructure.Services
{
    public class RoleManagementService : IRoleManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoleManagementService> _logger;

        public RoleManagementService(IUnitOfWork unitOfWork, ILogger<RoleManagementService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> PromoteToManagerAsync(int adminId, int userId, int teamId)
        {
            try
            {
                _logger.LogInformation("Admin {AdminId} promoting user {UserId} to manager of team {TeamId}", adminId, userId, teamId);

                // Validate admin permissions
                var admin = await _unitOfWork.Users.GetUserWithRolesAsync(adminId);
                if (admin == null || !admin.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                {
                    return ApiResponse<bool>.ErrorResult("Only admins can promote users to manager");
                }

                // Validate user exists
                var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                // Validate team exists
                var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
                if (team == null)
                {
                    return ApiResponse<bool>.ErrorResult("Team not found");
                }

                // Check if team already has a manager
                var currentManagerCount = await _unitOfWork.Teams.GetManagerCountForTeamAsync(teamId);
                if (currentManagerCount > 0)
                {
                    var currentManager = await _unitOfWork.Teams.GetTeamManagerAsync(teamId);
                    return ApiResponse<bool>.ErrorResult($"Team already has a manager: {currentManager?.Username}. Remove current manager first.");
                }

                // Check if user is already a manager of another team
                var userCurrentRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
                if (userCurrentRoles.Contains("Manager"))
                {
                    return ApiResponse<bool>.ErrorResult("User is already a manager of another team");
                }

                // Only User or Viewer can be promoted to Manager
                if (!userCurrentRoles.Any(r => r == "User" || r == "Viewer"))
                {
                    return ApiResponse<bool>.ErrorResult("Only User or Viewer roles can be promoted to Manager");
                }

                // Assign user to team
                user.TeamId = teamId;
                await _unitOfWork.Users.UpdateAsync(user);

                // Remove existing roles and assign Manager role
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(userId);

                var managerRole = await _unitOfWork.Roles.GetByNameAsync("Manager");
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = managerRole!.Id,
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = adminId,
                    Action = "User Promoted to Manager",
                    Metadata = $"User: {user.Username} promoted to manager of team: {team.Name}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully promoted user {Username} to manager of team {TeamName}", user.Username, team.Name);
                return ApiResponse<bool>.SuccessResult(true, "User promoted to manager successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting user {UserId} to manager", userId);
                return ApiResponse<bool>.ErrorResult($"Error promoting user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DemoteManagerAsync(int adminId, int managerId)
        {
            try
            {
                _logger.LogInformation("Admin {AdminId} demoting manager {ManagerId}", adminId, managerId);

                // Validate admin permissions
                var admin = await _unitOfWork.Users.GetUserWithRolesAsync(adminId);
                if (admin == null || !admin.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                {
                    return ApiResponse<bool>.ErrorResult("Only admins can demote managers");
                }

                // Validate manager exists and is actually a manager
                var manager = await _unitOfWork.Users.GetUserWithRolesAsync(managerId);
                if (manager == null)
                {
                    return ApiResponse<bool>.ErrorResult("Manager not found");
                }

                if (!manager.UserRoles.Any(ur => ur.Role.Name == "Manager"))
                {
                    return ApiResponse<bool>.ErrorResult("User is not a manager");
                }

                var teamName = manager.TeamId.HasValue ?
                    (await _unitOfWork.Teams.GetByIdAsync(manager.TeamId.Value))?.Name :
                    "Unknown";

                // Remove Manager role and assign User role
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(managerId);

                var userRole = await _unitOfWork.Roles.GetByNameAsync("User");
                var newUserRole = new UserRole
                {
                    UserId = managerId,
                    RoleId = userRole!.Id,
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(newUserRole);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = adminId,
                    Action = "Manager Demoted",
                    Metadata = $"Manager: {manager.Username} demoted from team: {teamName}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully demoted manager {Username}", manager.Username);
                return ApiResponse<bool>.SuccessResult(true, "Manager demoted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error demoting manager {ManagerId}", managerId);
                return ApiResponse<bool>.ErrorResult($"Error demoting manager: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ChangeUserRoleAsync(int managerId, int userId, string newRoleName)
        {
            try
            {
                _logger.LogInformation("Manager {ManagerId} changing role of user {UserId} to {NewRole}", managerId, userId, newRoleName);

                // Validate the role change is allowed
                var validation = await ValidateRoleChangeAsync(managerId, userId, newRoleName);
                if (!validation.Success)
                {
                    return validation;
                }

                var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
                var manager = await _unitOfWork.Users.GetUserWithRolesAsync(managerId);
                var oldRole = user!.UserRoles.FirstOrDefault()?.Role.Name ?? "None";

                // Remove existing roles and assign new role
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(userId);

                var newRole = await _unitOfWork.Roles.GetByNameAsync(newRoleName);
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = newRole!.Id,
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = managerId,
                    Action = "User Role Changed",
                    Metadata = $"User: {user.Username}, Role: {oldRole} → {newRoleName}, By: {manager!.Username}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully changed role of user {Username} to {NewRole}", user.Username, newRoleName);
                return ApiResponse<bool>.SuccessResult(true, $"User role changed to {newRoleName} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", userId);
                return ApiResponse<bool>.ErrorResult($"Error changing role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ValidateRoleChangeAsync(int requestingUserId, int targetUserId, string newRoleName)
        {
            try
            {
                var requestingUser = await _unitOfWork.Users.GetUserWithRolesAsync(requestingUserId);
                var targetUser = await _unitOfWork.Users.GetUserWithRolesAsync(targetUserId);

                if (requestingUser == null || targetUser == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                var requestingUserRole = requestingUser.UserRoles.FirstOrDefault()?.Role.Name;
                var targetUserRole = targetUser.UserRoles.FirstOrDefault()?.Role.Name;

                // Admin can promote/demote between User, Viewer, and Manager
                if (requestingUserRole == "Admin")
                {
                    if (newRoleName == "Manager")
                    {
                        return ApiResponse<bool>.ErrorResult("Use the promote-to-manager endpoint for manager promotion");
                    }
                    return ApiResponse<bool>.SuccessResult(true);
                }

                // Manager can only change User ↔ Viewer within their team
                if (requestingUserRole == "Manager")
                {
                    // Check if users are in the same team
                    if (requestingUser.TeamId == null || requestingUser.TeamId != targetUser.TeamId)
                    {
                        return ApiResponse<bool>.ErrorResult("Managers can only change roles of users in their team");
                    }

                    // Check if target user is User or Viewer
                    if (targetUserRole != "User" && targetUserRole != "Viewer")
                    {
                        return ApiResponse<bool>.ErrorResult("Managers can only change User and Viewer roles");
                    }

                    // Check if new role is User or Viewer
                    if (newRoleName != "User" && newRoleName != "Viewer")
                    {
                        return ApiResponse<bool>.ErrorResult("Managers can only assign User or Viewer roles");
                    }

                    return ApiResponse<bool>.SuccessResult(true);
                }

                return ApiResponse<bool>.ErrorResult("Insufficient permissions to change roles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating role change");
                return ApiResponse<bool>.ErrorResult($"Error validating role change: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<string>>> GetAvailableRolesForUserAsync(int requestingUserId, int targetUserId)
        {
            try
            {
                var requestingUser = await _unitOfWork.Users.GetUserWithRolesAsync(requestingUserId);
                var targetUser = await _unitOfWork.Users.GetUserWithRolesAsync(targetUserId);

                if (requestingUser == null || targetUser == null)
                {
                    return ApiResponse<List<string>>.ErrorResult("User not found");
                }

                var requestingUserRole = requestingUser.UserRoles.FirstOrDefault()?.Role.Name;
                var availableRoles = new List<string>();

                if (requestingUserRole == "Admin")
                {
                    availableRoles.AddRange(new[] { "User", "Viewer" });
                    // Note: Manager promotion is handled separately
                }
                else if (requestingUserRole == "Manager")
                {
                    // Only if target user is in the same team
                    if (requestingUser.TeamId != null && requestingUser.TeamId == targetUser.TeamId)
                    {
                        availableRoles.AddRange(new[] { "User", "Viewer" });
                    }
                }

                return ApiResponse<List<string>>.SuccessResult(availableRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available roles");
                return ApiResponse<List<string>>.ErrorResult($"Error getting available roles: {ex.Message}");
            }
        }
    }
}