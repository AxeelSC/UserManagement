using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Teams;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Infrastructure.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeamService> _logger;

        public TeamService(IUnitOfWork unitOfWork, ILogger<TeamService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<List<TeamSummaryDto>>> GetAllTeamsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all teams");

                var teams = await _unitOfWork.Teams.GetAllAsync();
                var teamDtos = new List<TeamSummaryDto>();

                foreach (var team in teams)
                {
                    var manager = await _unitOfWork.Teams.GetTeamManagerAsync(team.Id);
                    var memberCount = await _unitOfWork.Users.CountAsync(u => u.TeamId == team.Id);

                    teamDtos.Add(new TeamSummaryDto
                    {
                        Id = team.Id,
                        Name = team.Name,
                        Description = team.Description,
                        ManagerName = manager?.Username,
                        MemberCount = memberCount
                    });
                }

                _logger.LogInformation("Successfully retrieved {TeamCount} teams", teamDtos.Count);
                return ApiResponse<List<TeamSummaryDto>>.SuccessResult(teamDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all teams");
                return ApiResponse<List<TeamSummaryDto>>.ErrorResult($"Error retrieving teams: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> GetTeamByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving team with ID: {TeamId}", id);

                var team = await _unitOfWork.Teams.GetWithUsersAsync(id);
                if (team == null)
                {
                    _logger.LogWarning("Team not found with ID: {TeamId}", id);
                    return ApiResponse<TeamDto>.ErrorResult("Team not found");
                }

                var teamDto = await MapToTeamDtoAsync(team);

                _logger.LogInformation("Successfully retrieved team: {TeamName}", team.Name);
                return ApiResponse<TeamDto>.SuccessResult(teamDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team with ID: {TeamId}", id);
                return ApiResponse<TeamDto>.ErrorResult($"Error retrieving team: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> GetTeamByNameAsync(string name)
        {
            try
            {
                _logger.LogInformation("Retrieving team by name: {TeamName}", name);

                var team = await _unitOfWork.Teams.GetByNameAsync(name);
                if (team == null)
                {
                    _logger.LogWarning("Team not found with name: {TeamName}", name);
                    return ApiResponse<TeamDto>.ErrorResult("Team not found");
                }

                var teamWithUsers = await _unitOfWork.Teams.GetWithUsersAsync(team.Id);
                var teamDto = await MapToTeamDtoAsync(teamWithUsers!);

                _logger.LogInformation("Successfully retrieved team by name: {TeamName}", name);
                return ApiResponse<TeamDto>.SuccessResult(teamDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team by name: {TeamName}", name);
                return ApiResponse<TeamDto>.ErrorResult($"Error retrieving team: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto)
        {
            try
            {
                _logger.LogInformation("Creating team with name: {TeamName}", createTeamDto.Name);

                // Validation
                var existingTeam = await _unitOfWork.Teams.GetByNameAsync(createTeamDto.Name);
                if (existingTeam != null)
                {
                    _logger.LogWarning("Attempt to create team with duplicate name: {TeamName}", createTeamDto.Name);
                    return ApiResponse<TeamDto>.ErrorResult("Team name already exists");
                }

                // Create team
                var team = new Team
                {
                    Name = createTeamDto.Name,
                    Description = createTeamDto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Teams.AddAsync(team);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null, 
                    Action = "Team Created",
                    Metadata = $"Team: {team.Name}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var teamDto = await MapToTeamDtoAsync(team);

                _logger.LogInformation("Successfully created team: {TeamName} with ID: {TeamId}", team.Name, team.Id);
                return ApiResponse<TeamDto>.SuccessResult(teamDto, "Team created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team with name: {TeamName}", createTeamDto.Name);
                return ApiResponse<TeamDto>.ErrorResult($"Error creating team: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> UpdateTeamAsync(int id, UpdateTeamDto updateTeamDto)
        {
            try
            {
                _logger.LogInformation("Updating team with ID: {TeamId}", id);

                var team = await _unitOfWork.Teams.GetByIdAsync(id);
                if (team == null)
                {
                    _logger.LogWarning("Attempt to update non-existent team with ID: {TeamId}", id);
                    return ApiResponse<TeamDto>.ErrorResult("Team not found");
                }

                // Validation
                var existingTeam = await _unitOfWork.Teams.GetByNameAsync(updateTeamDto.Name);
                if (existingTeam != null && existingTeam.Id != id)
                {
                    _logger.LogWarning("Attempt to update team {TeamId} with duplicate name: {TeamName}", id, updateTeamDto.Name);
                    return ApiResponse<TeamDto>.ErrorResult("Team name already exists");
                }

                // Update team
                var oldName = team.Name;
                team.Name = updateTeamDto.Name;
                team.Description = updateTeamDto.Description;

                await _unitOfWork.Teams.UpdateAsync(team);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null,
                    Action = "Team Updated",
                    Metadata = $"Team ID: {id}, Name: {oldName} → {team.Name}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var teamWithUsers = await _unitOfWork.Teams.GetWithUsersAsync(id);
                var teamDto = await MapToTeamDtoAsync(teamWithUsers!);

                _logger.LogInformation("Successfully updated team: {TeamName} (ID: {TeamId})", team.Name, id);
                return ApiResponse<TeamDto>.SuccessResult(teamDto, "Team updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team with ID: {TeamId}", id);
                return ApiResponse<TeamDto>.ErrorResult($"Error updating team: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteTeamAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting team with ID: {TeamId}", id);

                var team = await _unitOfWork.Teams.GetWithUsersAsync(id);
                if (team == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent team with ID: {TeamId}", id);
                    return ApiResponse<bool>.ErrorResult("Team not found");
                }

                // Check if team has members
                if (team.Users.Any())
                {
                    _logger.LogWarning("Attempt to delete team {TeamName} (ID: {TeamId}) that has {MemberCount} members",
                        team.Name, id, team.Users.Count);
                    return ApiResponse<bool>.ErrorResult("Cannot delete team that has members. Please reassign or remove all members first.");
                }

                await _unitOfWork.Teams.DeleteAsync(team);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null,
                    Action = "Team Deleted",
                    Metadata = $"Team: {team.Name}, ID: {id}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted team: {TeamName} (ID: {TeamId})", team.Name, id);
                return ApiResponse<bool>.SuccessResult(true, "Team deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team with ID: {TeamId}", id);
                return ApiResponse<bool>.ErrorResult($"Error deleting team: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TeamSummaryDto>>> GetTeamsForManagerAsync(int managerId)
        {
            try
            {
                _logger.LogInformation("Retrieving teams for manager ID: {ManagerId}", managerId);

                var manager = await _unitOfWork.Users.GetByIdAsync(managerId);
                if (manager == null || manager.TeamId == null)
                {
                    return ApiResponse<List<TeamSummaryDto>>.ErrorResult("Manager not found or not assigned to a team");
                }

                var team = await _unitOfWork.Teams.GetByIdAsync(manager.TeamId.Value);
                if (team == null)
                {
                    return ApiResponse<List<TeamSummaryDto>>.ErrorResult("Team not found");
                }

                var memberCount = await _unitOfWork.Users.CountAsync(u => u.TeamId == team.Id);
                var teamDto = new TeamSummaryDto
                {
                    Id = team.Id,
                    Name = team.Name,
                    Description = team.Description,
                    ManagerName = manager.Username,
                    MemberCount = memberCount
                };

                _logger.LogInformation("Successfully retrieved team for manager: {TeamName}", team.Name);
                return ApiResponse<List<TeamSummaryDto>>.SuccessResult(new List<TeamSummaryDto> { teamDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teams for manager ID: {ManagerId}", managerId);
                return ApiResponse<List<TeamSummaryDto>>.ErrorResult($"Error retrieving teams: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> AssignManagerAsync(int teamId, int userId)
        {
            try
            {
                _logger.LogInformation("Assigning manager {UserId} to team {TeamId}", userId, teamId);

                var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
                if (team == null)
                {
                    return ApiResponse<bool>.ErrorResult("Team not found");
                }

                var user = await _unitOfWork.Users.GetUserWithRolesAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                // Check if team already has a manager
                var currentManagerCount = await _unitOfWork.Teams.GetManagerCountForTeamAsync(teamId);
                if (currentManagerCount > 0)
                {
                    return ApiResponse<bool>.ErrorResult("Team already has a manager. Please remove the current manager first.");
                }

                // Assign user to team
                user.TeamId = teamId;
                await _unitOfWork.Users.UpdateAsync(user);

                // Assign manager role (remove existing roles and add manager role)
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(userId);

                var managerRole = await _unitOfWork.Roles.GetByNameAsync("Manager");
                if (managerRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = userId,
                        RoleId = managerRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(userRole);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully assigned manager {Username} to team {TeamName}", user.Username, team.Name);
                return ApiResponse<bool>.SuccessResult(true, "Manager assigned successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning manager {UserId} to team {TeamId}", userId, teamId);
                return ApiResponse<bool>.ErrorResult($"Error assigning manager: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> RemoveManagerAsync(int teamId)
        {
            try
            {
                _logger.LogInformation("Removing manager from team {TeamId}", teamId);

                var manager = await _unitOfWork.Teams.GetTeamManagerAsync(teamId);
                if (manager == null)
                {
                    return ApiResponse<bool>.ErrorResult("No manager found for this team");
                }

                // Remove manager role and assign User role
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(manager.Id);

                var userRole = await _unitOfWork.Roles.GetByNameAsync("User");
                if (userRole != null)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = manager.Id,
                        RoleId = userRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(newUserRole);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully removed manager {Username} from team {TeamId}", manager.Username, teamId);
                return ApiResponse<bool>.SuccessResult(true, "Manager removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing manager from team {TeamId}", teamId);
                return ApiResponse<bool>.ErrorResult($"Error removing manager: {ex.Message}");
            }
        }

        private async Task<TeamDto> MapToTeamDtoAsync(Team team)
        {
            var manager = await _unitOfWork.Teams.GetTeamManagerAsync(team.Id);

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                CreatedAt = team.CreatedAt,
                Manager = manager != null ? new UserSummaryDto
                {
                    Id = manager.Id,
                    Username = manager.Username,
                    Email = manager.Email,
                    IsActive = manager.IsActive
                } : null,
                Members = team.Users?.Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive
                }).ToList() ?? new List<UserSummaryDto>(),
                MemberCount = team.Users?.Count ?? 0
            };
        }
    }
}
