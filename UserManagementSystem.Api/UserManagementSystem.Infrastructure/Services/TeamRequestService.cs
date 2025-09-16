using Microsoft.Extensions.Logging;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Teams;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;
using UserManagementSystem.Domain.Enums;

namespace UserManagementSystem.Infrastructure.Services
{
    public class TeamRequestService : ITeamRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TeamRequestService> _logger;

        public TeamRequestService(IUnitOfWork unitOfWork, ILogger<TeamRequestService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<TeamRequestDto>> CreateRequestAsync(int userId, CreateTeamRequestDto createRequestDto)
        {
            try
            {
                _logger.LogInformation("Creating team request for user {UserId} to join team {TeamId}", userId, createRequestDto.TeamId);

                // Validation
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<TeamRequestDto>.ErrorResult("User not found");
                }

                var team = await _unitOfWork.Teams.GetByIdAsync(createRequestDto.TeamId);
                if (team == null)
                {
                    return ApiResponse<TeamRequestDto>.ErrorResult("Team not found");
                }

                // Check if user already has a team
                if (user.TeamId != null)
                {
                    _logger.LogWarning("User {UserId} already belongs to a team", userId);
                    return ApiResponse<TeamRequestDto>.ErrorResult("You already belong to a team. Please leave your current team first.");
                }

                // Check if there's already a pending request
                var existingRequest = await _unitOfWork.TeamRequests.GetPendingRequestByUserAndTeamAsync(userId, createRequestDto.TeamId);
                if (existingRequest != null)
                {
                    _logger.LogWarning("User {UserId} already has a pending request for team {TeamId}", userId, createRequestDto.TeamId);
                    return ApiResponse<TeamRequestDto>.ErrorResult("You already have a pending request for this team");
                }

                // Check if team has a manager
                var manager = await _unitOfWork.Teams.GetTeamManagerAsync(createRequestDto.TeamId);
                if (manager == null)
                {
                    _logger.LogWarning("Team {TeamId} has no manager to process requests", createRequestDto.TeamId);
                    return ApiResponse<TeamRequestDto>.ErrorResult("This team has no manager to process requests");
                }

                // Create the request
                var teamRequest = new TeamRequest
                {
                    UserId = userId,
                    TeamId = createRequestDto.TeamId,
                    Message = createRequestDto.Message,
                    Status = TeamRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                await _unitOfWork.TeamRequests.AddAsync(teamRequest);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Team Request Created",
                    Metadata = $"User: {user.Username}, Team: {team.Name}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                // Get the created request with full details
                var createdRequest = await _unitOfWork.TeamRequests.GetByIdAsync(teamRequest.Id);
                var requestDto = MapToTeamRequestDto(createdRequest!, user, team, null);

                _logger.LogInformation("Successfully created team request for user {Username} to join team {TeamName}", user.Username, team.Name);
                return ApiResponse<TeamRequestDto>.SuccessResult(requestDto, "Team request created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team request for user {UserId} to join team {TeamId}", userId, createRequestDto.TeamId);
                return ApiResponse<TeamRequestDto>.ErrorResult($"Error creating team request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TeamRequestDto>>> GetRequestsByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving team requests for user {UserId}", userId);

                var requests = await _unitOfWork.TeamRequests.GetRequestsByUserAsync(userId);
                var requestDtos = requests.Select(r => MapToTeamRequestDto(r, r.User, r.Team, r.ProcessedByUser)).ToList();

                _logger.LogInformation("Successfully retrieved {RequestCount} team requests for user {UserId}", requestDtos.Count, userId);
                return ApiResponse<List<TeamRequestDto>>.SuccessResult(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team requests for user {UserId}", userId);
                return ApiResponse<List<TeamRequestDto>>.ErrorResult($"Error retrieving requests: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TeamRequestDto>>> GetPendingRequestsForTeamAsync(int teamId)
        {
            try
            {
                _logger.LogInformation("Retrieving pending requests for team {TeamId}", teamId);

                var requests = await _unitOfWork.TeamRequests.GetPendingRequestsForTeamAsync(teamId);
                var requestDtos = requests.Select(r => MapToTeamRequestDto(r, r.User, r.Team, null)).ToList();

                _logger.LogInformation("Successfully retrieved {RequestCount} pending requests for team {TeamId}", requestDtos.Count, teamId);
                return ApiResponse<List<TeamRequestDto>>.SuccessResult(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending requests for team {TeamId}", teamId);
                return ApiResponse<List<TeamRequestDto>>.ErrorResult($"Error retrieving requests: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TeamRequestDto>>> GetTeamMailboxAsync(int managerId)
        {
            try
            {
                _logger.LogInformation("Retrieving team mailbox for manager {ManagerId}", managerId);

                var manager = await _unitOfWork.Users.GetByIdAsync(managerId);
                if (manager?.TeamId == null)
                {
                    return ApiResponse<List<TeamRequestDto>>.ErrorResult("Manager not found or not assigned to a team");
                }

                var requests = await _unitOfWork.TeamRequests.GetPendingRequestsForTeamAsync(manager.TeamId.Value);
                var requestDtos = requests.Select(r => MapToTeamRequestDto(r, r.User, r.Team, null)).ToList();

                _logger.LogInformation("Successfully retrieved {RequestCount} requests in mailbox for manager {ManagerId}", requestDtos.Count, managerId);
                return ApiResponse<List<TeamRequestDto>>.SuccessResult(requestDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team mailbox for manager {ManagerId}", managerId);
                return ApiResponse<List<TeamRequestDto>>.ErrorResult($"Error retrieving mailbox: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamRequestDto>> ProcessRequestAsync(int requestId, int processedByUserId, ProcessTeamRequestDto processDto)
        {
            try
            {
                _logger.LogInformation("Processing team request {RequestId} by user {ProcessedByUserId}", requestId, processedByUserId);

                var request = await _unitOfWork.TeamRequests.GetByIdAsync(requestId);
                if (request == null)
                {
                    return ApiResponse<TeamRequestDto>.ErrorResult("Request not found");
                }

                if (request.Status != TeamRequestStatus.Pending)
                {
                    return ApiResponse<TeamRequestDto>.ErrorResult("Request has already been processed");
                }

                var processedByUser = await _unitOfWork.Users.GetByIdAsync(processedByUserId);
                if (processedByUser == null)
                {
                    return ApiResponse<TeamRequestDto>.ErrorResult("Processing user not found");
                }

                // Update request status
                request.Status = processDto.Approve ? TeamRequestStatus.Approved : TeamRequestStatus.Rejected;
                request.ProcessedAt = DateTime.UtcNow;
                request.ProcessedByUserId = processedByUserId;
                request.ProcessingNotes = processDto.Notes;

                await _unitOfWork.TeamRequests.UpdateAsync(request);

                // If approved, assign user to team
                if (processDto.Approve)
                {
                    var requestingUser = await _unitOfWork.Users.GetByIdAsync(request.UserId);
                    if (requestingUser != null)
                    {
                        requestingUser.TeamId = request.TeamId;
                        await _unitOfWork.Users.UpdateAsync(requestingUser);
                    }
                }

                // Log the action
                var action = processDto.Approve ? "Team Request Approved" : "Team Request Rejected";
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = processedByUserId,
                    Action = action,
                    Metadata = $"Request ID: {requestId}, Notes: {processDto.Notes}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                // Get updated request with full details
                var updatedRequest = await _unitOfWork.TeamRequests.GetByIdAsync(requestId);
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
                var team = await _unitOfWork.Teams.GetByIdAsync(request.TeamId);
                var requestDto = MapToTeamRequestDto(updatedRequest!, user!, team!, processedByUser);

                var statusText = processDto.Approve ? "approved" : "rejected";
                _logger.LogInformation("Successfully {Status} team request {RequestId}", statusText, requestId);
                return ApiResponse<TeamRequestDto>.SuccessResult(requestDto, $"Request {statusText} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing team request {RequestId}", requestId);
                return ApiResponse<TeamRequestDto>.ErrorResult($"Error processing request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelRequestAsync(int requestId, int userId)
        {
            try
            {
                _logger.LogInformation("Canceling team request {RequestId} by user {UserId}", requestId, userId);

                var request = await _unitOfWork.TeamRequests.GetByIdAsync(requestId);
                if (request == null)
                {
                    return ApiResponse<bool>.ErrorResult("Request not found");
                }

                if (request.UserId != userId)
                {
                    return ApiResponse<bool>.ErrorResult("You can only cancel your own requests");
                }

                if (request.Status != TeamRequestStatus.Pending)
                {
                    return ApiResponse<bool>.ErrorResult("Only pending requests can be canceled");
                }

                await _unitOfWork.TeamRequests.DeleteAsync(request);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Team Request Canceled",
                    Metadata = $"Request ID: {requestId}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully canceled team request {RequestId}", requestId);
                return ApiResponse<bool>.SuccessResult(true, "Request canceled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling team request {RequestId}", requestId);
                return ApiResponse<bool>.ErrorResult($"Error canceling request: {ex.Message}");
            }
        }

        private TeamRequestDto MapToTeamRequestDto(TeamRequest request, User user, Team team, User? processedByUser)
        {
            return new TeamRequestDto
            {
                Id = request.Id,
                UserId = request.UserId,
                Username = user.Username,
                UserEmail = user.Email,
                TeamId = request.TeamId,
                TeamName = team.Name,
                Message = request.Message,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                ProcessedAt = request.ProcessedAt,
                ProcessedBy = processedByUser?.Username,
                ProcessingNotes = request.ProcessingNotes
            };
        }
    }
}