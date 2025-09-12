using Microsoft.Extensions.Logging;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Infrastructure.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IUnitOfWork unitOfWork, ILogger<RoleService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<RoleDto>> GetRoleByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving role with ID: {RoleId}", id);

                var role = await _unitOfWork.Roles.GetByIdAsync(id);

                if (role == null)
                {
                    _logger.LogWarning("Role not found with ID: {RoleId}", id);
                    return ApiResponse<RoleDto>.ErrorResult("Role not found");
                }

                _logger.LogInformation("Successfully retrieved role: {RoleName} (ID: {RoleId})", role.Name, id);
                var roleDto = MapToRoleDto(role);
                return ApiResponse<RoleDto>.SuccessResult(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role with ID: {RoleId}", id);
                return ApiResponse<RoleDto>.ErrorResult($"Error retrieving role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all roles");

                var roles = await _unitOfWork.Roles.GetAllAsync();
                var roleDtos = roles.Select(MapToRoleDto).ToList();

                _logger.LogInformation("Successfully retrieved {RoleCount} roles", roleDtos.Count);
                return ApiResponse<List<RoleDto>>.SuccessResult(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                return ApiResponse<List<RoleDto>>.ErrorResult($"Error retrieving roles: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto)
        {
            try
            {
                _logger.LogInformation("Creating role with name: {RoleName}", createRoleDto.Name);

                if (!await _unitOfWork.Roles.IsNameUniqueAsync(createRoleDto.Name))
                {
                    _logger.LogWarning("Attempt to create role with duplicate name: {RoleName}", createRoleDto.Name);
                    return ApiResponse<RoleDto>.ErrorResult("Role name already exists");
                }

                var role = new Role
                {
                    Name = createRoleDto.Name,
                    Description = createRoleDto.Description
                };

                _logger.LogDebug("Adding role to database: {RoleName}", role.Name);
                await _unitOfWork.Roles.AddAsync(role);

                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null, 
                    Action = "Role Created",
                    Metadata = $"Role: {role.Name}, Description: {role.Description}",
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogDebug("Saving changes to database");
                await _unitOfWork.SaveChangesAsync();

                var roleDto = MapToRoleDto(role);
                _logger.LogInformation("Successfully created role: {RoleName} with ID: {RoleId}", role.Name, role.Id);
                return ApiResponse<RoleDto>.SuccessResult(roleDto, "Role created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role with name: {RoleName}", createRoleDto.Name);
                return ApiResponse<RoleDto>.ErrorResult($"Error creating role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RoleDto>> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto)
        {
            try
            {
                _logger.LogInformation("Updating role with ID: {RoleId}", id);

                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Attempt to update non-existent role with ID: {RoleId}", id);
                    return ApiResponse<RoleDto>.ErrorResult("Role not found");
                }

                _logger.LogDebug("Found role: {RoleName}, updating with new data", role.Name);

                if (!await _unitOfWork.Roles.IsNameUniqueAsync(updateRoleDto.Name, id))
                {
                    _logger.LogWarning("Attempt to update role {RoleId} with duplicate name: {RoleName}", id, updateRoleDto.Name);
                    return ApiResponse<RoleDto>.ErrorResult("Role name already exists");
                }

                var oldName = role.Name;
                var oldDescription = role.Description;

                role.Name = updateRoleDto.Name;
                role.Description = updateRoleDto.Description;

                await _unitOfWork.Roles.UpdateAsync(role);

                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null,
                    Action = "Role Updated",
                    Metadata = $"Role ID: {id}, Name: {oldName} → {role.Name}, Description: {oldDescription} → {role.Description}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var roleDto = MapToRoleDto(role);
                _logger.LogInformation("Successfully updated role: {RoleName} (ID: {RoleId})", role.Name, id);
                return ApiResponse<RoleDto>.SuccessResult(roleDto, "Role updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role with ID: {RoleId}", id);
                return ApiResponse<RoleDto>.ErrorResult($"Error updating role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteRoleAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting role with ID: {RoleId}", id);

                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent role with ID: {RoleId}", id);
                    return ApiResponse<bool>.ErrorResult("Role not found");
                }

                var roleName = role.Name;
                _logger.LogDebug("Found role: {RoleName}, checking for assigned users", roleName);

                var usersWithRole = await _unitOfWork.UserRoles.GetByRoleIdAsync(id);
                if (usersWithRole.Any())
                {
                    _logger.LogWarning("Attempt to delete role {RoleName} (ID: {RoleId}) that is assigned to {UserCount} users",
                        roleName, id, usersWithRole.Count());
                    return ApiResponse<bool>.ErrorResult("Cannot delete role that is assigned to users");
                }

                _logger.LogDebug("Role {RoleName} is not assigned to any users, proceeding with deletion", roleName);
                await _unitOfWork.Roles.DeleteAsync(role);

                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null,
                    Action = "Role Deleted",
                    Metadata = $"Role: {roleName}, ID: {id}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted role: {RoleName} (ID: {RoleId})", roleName, id);
                return ApiResponse<bool>.SuccessResult(true, "Role deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with ID: {RoleId}", id);
                return ApiResponse<bool>.ErrorResult($"Error deleting role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RoleDto>> GetRoleByNameAsync(string name)
        {
            try
            {
                _logger.LogInformation("Retrieving role by name: {RoleName}", name);

                var role = await _unitOfWork.Roles.GetByNameAsync(name);

                if (role == null)
                {
                    _logger.LogWarning("Role not found with name: {RoleName}", name);
                    return ApiResponse<RoleDto>.ErrorResult("Role not found");
                }

                var roleDto = MapToRoleDto(role);
                _logger.LogInformation("Successfully retrieved role by name: {RoleName} (ID: {RoleId})", name, role.Id);
                return ApiResponse<RoleDto>.SuccessResult(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role by name: {RoleName}", name);
                return ApiResponse<RoleDto>.ErrorResult($"Error retrieving role: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<RoleDto>>> GetRolesByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving roles for user ID: {UserId}", userId);

                var roles = await _unitOfWork.Roles.GetRolesByUserIdAsync(userId);
                var roleDtos = roles.Select(MapToRoleDto).ToList();

                _logger.LogInformation("Successfully retrieved {RoleCount} roles for user ID: {UserId}", roleDtos.Count, userId);
                return ApiResponse<List<RoleDto>>.SuccessResult(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user ID: {UserId}", userId);
                return ApiResponse<List<RoleDto>>.ErrorResult($"Error retrieving user roles: {ex.Message}");
            }
        }

        private RoleDto MapToRoleDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };
        }
    }
}