using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Auth;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Application.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequestDto request);
        string GenerateJwtToken(User user);
        Task<User?> GetUserByTokenAsync(string token);
    }
}
