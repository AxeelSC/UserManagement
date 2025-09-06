using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;


namespace UserManagementSystem.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordService _passwordService;

        public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _passwordService = passwordService; 
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving user with ID: {UserId}", id);

                var user = await _unitOfWork.Users.GetUserWithRolesAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return ApiResponse<UserDto>.ErrorResult("User not found");
                }

                _logger.LogInformation("Successfully retrieved user: {Username} (ID: {UserId})", user.Username, id);
                var userDto = MapToUserDto(user);
                return ApiResponse<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return ApiResponse<UserDto>.ErrorResult($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<UserSummaryDto>>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");

                var users = await _unitOfWork.Users.GetAllAsync();
                var userDtos = users.Select(MapToUserSummaryDto).ToList();

                _logger.LogInformation("Successfully retrieved {UserCount} users", userDtos.Count);
                return ApiResponse<List<UserSummaryDto>>.SuccessResult(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                return ApiResponse<List<UserSummaryDto>>.ErrorResult($"Error retrieving users: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<UserSummaryDto>>> GetActiveUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all active users");

                var users = await _unitOfWork.Users.GetActiveUsersAsync();
                var userDtos = users.Select(MapToUserSummaryDto).ToList();

                _logger.LogInformation("Successfully retrieved {ActiveUserCount} active users", userDtos.Count);
                return ApiResponse<List<UserSummaryDto>>.SuccessResult(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active users");
                return ApiResponse<List<UserSummaryDto>>.ErrorResult($"Error retrieving active users: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                _logger.LogInformation("Creating user with username: {Username}, email: {Email}",
                    createUserDto.Username, createUserDto.Email);

                // Validation
                if (!await _unitOfWork.Users.IsUsernameUniqueAsync(createUserDto.Username))
                {
                    _logger.LogWarning("Attempt to create user with duplicate username: {Username}", createUserDto.Username);
                    return ApiResponse<UserDto>.ErrorResult("Username already exists");
                }

                if (!await _unitOfWork.Users.IsEmailUniqueAsync(createUserDto.Email))
                {
                    _logger.LogWarning("Attempt to create user with duplicate email: {Email}", createUserDto.Email);
                    return ApiResponse<UserDto>.ErrorResult("Email already exists");
                }

                // Password strength validation
                if (!_passwordService.IsPasswordStrong(createUserDto.Password))
                {
                    _logger.LogWarning("Weak password provided for user: {Username}", createUserDto.Username);
                    return ApiResponse<UserDto>.ErrorResult("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
                }
                // Create user entity
                var user = new User
                {
                    Username = createUserDto.Username,
                    Email = createUserDto.Email,
                    PasswordHash = HashPassword(createUserDto.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = createUserDto.IsActive
                };

                _logger.LogDebug("Adding user to database");
                await _unitOfWork.Users.AddAsync(user);

                // Add roles if specified
                _logger.LogDebug("Assigning {RoleCount} roles to user", createUserDto.RoleIds.Count);
                foreach (var roleId in createUserDto.RoleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(userRole);
                }

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = user.Id,
                    Action = "User Created",
                    Metadata = $"Username: {user.Username}, Email: {user.Email}",
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogDebug("Saving changes to database");
                await _unitOfWork.SaveChangesAsync();

                // Return the created user
                var createdUser = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
                var userDto = MapToUserDto(createdUser!);

                _logger.LogInformation("Successfully created user: {Username} with ID: {UserId}", user.Username, user.Id);
                return ApiResponse<UserDto>.SuccessResult(userDto, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with username: {Username}", createUserDto.Username);
                return ApiResponse<UserDto>.ErrorResult($"Error creating user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {UserId}", id);

                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to update non-existent user with ID: {UserId}", id);
                    return ApiResponse<UserDto>.ErrorResult("User not found");
                }

                _logger.LogDebug("Found user: {Username}, updating with new data", user.Username);

                // Validation
                if (!await _unitOfWork.Users.IsUsernameUniqueAsync(updateUserDto.Username, id))
                {
                    _logger.LogWarning("Attempt to update user {UserId} with duplicate username: {Username}", id, updateUserDto.Username);
                    return ApiResponse<UserDto>.ErrorResult("Username already exists");
                }

                if (!await _unitOfWork.Users.IsEmailUniqueAsync(updateUserDto.Email, id))
                {
                    _logger.LogWarning("Attempt to update user {UserId} with duplicate email: {Email}", id, updateUserDto.Email);
                    return ApiResponse<UserDto>.ErrorResult("Email already exists");
                }

                // Update user properties
                var oldUsername = user.Username;
                var oldEmail = user.Email;

                user.Username = updateUserDto.Username;
                user.Email = updateUserDto.Email;
                user.IsActive = updateUserDto.IsActive;

                await _unitOfWork.Users.UpdateAsync(user);

                // Update roles
                _logger.LogDebug("Updating roles for user {UserId}", id);
                await _unitOfWork.UserRoles.RemoveAllUserRolesAsync(id);
                foreach (var roleId in updateUserDto.RoleIds)
                {
                    var userRole = new UserRole
                    {
                        UserId = id,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserRoles.AddAsync(userRole);
                }

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = id,
                    Action = "User Updated",
                    Metadata = $"Username: {oldUsername} → {user.Username}, Email: {oldEmail} → {user.Email}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                var updatedUser = await _unitOfWork.Users.GetUserWithRolesAsync(id);
                var userDto = MapToUserDto(updatedUser!);

                _logger.LogInformation("Successfully updated user: {Username} (ID: {UserId})", user.Username, id);
                return ApiResponse<UserDto>.SuccessResult(userDto, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
                return ApiResponse<UserDto>.ErrorResult($"Error updating user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting user with ID: {UserId}", id);

                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent user with ID: {UserId}", id);
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                var username = user.Username;
                _logger.LogDebug("Deleting user: {Username} (ID: {UserId})", username, id);

                await _unitOfWork.Users.DeleteAsync(user);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = null, // User will be deleted
                    Action = "User Deleted",
                    Metadata = $"Username: {username}, ID: {id}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted user: {Username} (ID: {UserId})", username, id);
                return ApiResponse<bool>.SuccessResult(true, "User deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                return ApiResponse<bool>.ErrorResult($"Error deleting user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByUsernameAsync(string username)
        {
            try
            {
                _logger.LogInformation("Retrieving user by username: {Username}", username);

                var user = await _unitOfWork.Users.GetByUsernameAsync(username);

                if (user == null)
                {
                    _logger.LogWarning("User not found with username: {Username}", username);
                    return ApiResponse<UserDto>.ErrorResult("User not found");
                }

                var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
                var userDto = MapToUserDto(userWithRoles!);

                _logger.LogInformation("Successfully retrieved user by username: {Username} (ID: {UserId})", username, user.Id);
                return ApiResponse<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                return ApiResponse<UserDto>.ErrorResult($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Retrieving user by email: {Email}", email);

                var user = await _unitOfWork.Users.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return ApiResponse<UserDto>.ErrorResult("User not found");
                }

                var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
                var userDto = MapToUserDto(userWithRoles!);

                _logger.LogInformation("Successfully retrieved user by email: {Email} (ID: {UserId})", email, user.Id);
                return ApiResponse<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                return ApiResponse<UserDto>.ErrorResult($"Error retrieving user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                _logger.LogInformation("Changing password for user ID: {UserId}", userId);

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to change password for non-existent user ID: {UserId}", userId);
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                // Verify current password
                if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid current password provided for user: {Username} (ID: {UserId})", user.Username, userId);
                    return ApiResponse<bool>.ErrorResult("Current password is incorrect");
                }

                // Update password
                user.PasswordHash = HashPassword(changePasswordDto.NewPassword);
                await _unitOfWork.Users.UpdateAsync(user);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Password Changed",
                    Metadata = $"User: {user.Username}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully changed password for user: {Username} (ID: {UserId})", user.Username, userId);
                return ApiResponse<bool>.SuccessResult(true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID: {UserId}", userId);
                return ApiResponse<bool>.ErrorResult($"Error changing password: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ActivateUserAsync(int id)
        {
            try
            {
                _logger.LogInformation("Activating user with ID: {UserId}", id);

                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to activate non-existent user with ID: {UserId}", id);
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                if (user.IsActive)
                {
                    _logger.LogInformation("User {Username} (ID: {UserId}) is already active", user.Username, id);
                    return ApiResponse<bool>.SuccessResult(true, "User is already active");
                }

                user.IsActive = true;
                await _unitOfWork.Users.UpdateAsync(user);

                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = id,
                    Action = "User Activated",
                    Metadata = $"Username: {user.Username}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully activated user: {Username} (ID: {UserId})", user.Username, id);
                return ApiResponse<bool>.SuccessResult(true, "User activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user with ID: {UserId}", id);
                return ApiResponse<bool>.ErrorResult($"Error activating user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeactivateUserAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deactivating user with ID: {UserId}", id);

                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempt to deactivate non-existent user with ID: {UserId}", id);
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                if (!user.IsActive)
                {
                    _logger.LogInformation("User {Username} (ID: {UserId}) is already inactive", user.Username, id);
                    return ApiResponse<bool>.SuccessResult(true, "User is already inactive");
                }

                user.IsActive = false;
                await _unitOfWork.Users.UpdateAsync(user);

                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = id,
                    Action = "User Deactivated",
                    Metadata = $"Username: {user.Username}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deactivated user: {Username} (ID: {UserId})", user.Username, id);
                return ApiResponse<bool>.SuccessResult(true, "User deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user with ID: {UserId}", id);
                return ApiResponse<bool>.ErrorResult($"Error deactivating user: {ex.Message}");
            }
        }

        // Helper methods for mapping
        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Roles = user.UserRoles?.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                }).ToList() ?? new List<RoleDto>()
            };
        }

        private UserSummaryDto MapToUserSummaryDto(User user)
        {
            return new UserSummaryDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive
            };
        }
        private string HashPassword(string password)
        {
            return _passwordService.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return _passwordService.VerifyPassword(password, hash);
        }
    }
}