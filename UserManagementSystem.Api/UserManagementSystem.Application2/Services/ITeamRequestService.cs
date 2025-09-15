using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Teams;

namespace UserManagementSystem.Application.Services
{
    public interface ITeamRequestService
    {
        Task<ApiResponse<TeamRequestDto>> CreateRequestAsync(int userId, CreateTeamRequestDto createRequestDto);
        Task<ApiResponse<List<TeamRequestDto>>> GetRequestsByUserAsync(int userId);
        Task<ApiResponse<List<TeamRequestDto>>> GetPendingRequestsForTeamAsync(int teamId);
        Task<ApiResponse<List<TeamRequestDto>>> GetTeamMailboxAsync(int managerId);
        Task<ApiResponse<TeamRequestDto>> ProcessRequestAsync(int requestId, int processedByUserId, ProcessTeamRequestDto processDto);
        Task<ApiResponse<bool>> CancelRequestAsync(int requestId, int userId);
    }
}
