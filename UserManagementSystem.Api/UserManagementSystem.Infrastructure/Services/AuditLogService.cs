using Microsoft.Extensions.Logging;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IUnitOfWork unitOfWork, ILogger<AuditLogService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving audit logs for user ID: {UserId}", userId);

                var auditLogs = await _unitOfWork.AuditLogs.GetByUserIdAsync(userId);
                var auditLogDtos = auditLogs.Select(MapToAuditLogDto).ToList();

                _logger.LogInformation("Successfully retrieved {AuditLogCount} audit logs for user ID: {UserId}", auditLogDtos.Count, userId);
                return ApiResponse<List<AuditLogDto>>.SuccessResult(auditLogDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for user ID: {UserId}", userId);
                return ApiResponse<List<AuditLogDto>>.ErrorResult($"Error retrieving audit logs: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByActionAsync(string action)
        {
            try
            {
                _logger.LogInformation("Retrieving audit logs for action: {Action}", action);

                var auditLogs = await _unitOfWork.AuditLogs.GetByActionAsync(action);
                var auditLogDtos = auditLogs.Select(MapToAuditLogDto).ToList();

                _logger.LogInformation("Successfully retrieved {AuditLogCount} audit logs for action: {Action}", auditLogDtos.Count, action);
                return ApiResponse<List<AuditLogDto>>.SuccessResult(auditLogDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for action: {Action}", action);
                return ApiResponse<List<AuditLogDto>>.ErrorResult($"Error retrieving audit logs: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AuditLogDto>>> GetRecentAuditLogsAsync(int count = 100)
        {
            try
            {
                _logger.LogInformation("Retrieving {Count} recent audit logs", count);

                var auditLogs = await _unitOfWork.AuditLogs.GetRecentLogsAsync(count);
                var auditLogDtos = auditLogs.Select(MapToAuditLogDto).ToList();

                _logger.LogInformation("Successfully retrieved {AuditLogCount} recent audit logs", auditLogDtos.Count);
                return ApiResponse<List<AuditLogDto>>.SuccessResult(auditLogDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent audit logs (count: {Count})", count);
                return ApiResponse<List<AuditLogDto>>.ErrorResult($"Error retrieving recent audit logs: {ex.Message}");
            }
        }

        public async Task LogActionAsync(int? userId, string action, string? metadata = null)
        {
            try
            {
                _logger.LogDebug("Logging audit action: {Action} for user: {UserId}", action, userId ?? 0);

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow
                };

                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Successfully logged audit action: {Action} for user: {UserId}", action, userId ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit action: {Action} for user: {UserId}", action, userId ?? 0);
            }
        }

        private AuditLogDto MapToAuditLogDto(AuditLog auditLog)
        {
            return new AuditLogDto
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                Username = auditLog.User?.Username, 
                Action = auditLog.Action,
                Metadata = auditLog.Metadata,
                Timestamp = auditLog.Timestamp
            };
        }
    }
}