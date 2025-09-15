using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Teams;

namespace UserManagementSystem.Application.Services
{
    public interface ITeamService
    {
        Task<ApiResponse<List<TeamSummaryDto>>> GetAllTeamsAsync();
        Task<ApiResponse<TeamDto>> GetTeamByIdAsync(int id);
        Task<ApiResponse<TeamDto>> GetTeamByNameAsync(string name);
        Task<ApiResponse<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto);
        Task<ApiResponse<TeamDto>> UpdateTeamAsync(int id, UpdateTeamDto updateTeamDto);
        Task<ApiResponse<bool>> DeleteTeamAsync(int id);
        Task<ApiResponse<List<TeamSummaryDto>>> GetTeamsForManagerAsync(int managerId);
        Task<ApiResponse<bool>> AssignManagerAsync(int teamId, int userId);
        Task<ApiResponse<bool>> RemoveManagerAsync(int teamId);
    }
}
