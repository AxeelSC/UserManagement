using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;

namespace UserManagementSystem.Application.Services
{
    public interface IUserService
    {
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
        Task<ApiResponse<List<UserSummaryDto>>> GetAllUsersAsync();
        Task<ApiResponse<List<UserSummaryDto>>> GetActiveUsersAsync();
        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
        Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<ApiResponse<bool>> DeleteUserAsync(int id);
        Task<ApiResponse<UserDto>> GetUserByUsernameAsync(string username);
        Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email);
        Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<ApiResponse<bool>> ActivateUserAsync(int id);
        Task<ApiResponse<bool>> DeactivateUserAsync(int id);
    }
}
