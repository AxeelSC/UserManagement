using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;

namespace UserManagementSystem.Application.Services
{
    public interface IRoleService
    {
        Task<ApiResponse<RoleDto>> GetRoleByIdAsync(int id);
        Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();
        Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);
        Task<ApiResponse<RoleDto>> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto);
        Task<ApiResponse<bool>> DeleteRoleAsync(int id);
        Task<ApiResponse<RoleDto>> GetRoleByNameAsync(string name);
        Task<ApiResponse<List<RoleDto>>> GetRolesByUserIdAsync(int userId);
    }
}
