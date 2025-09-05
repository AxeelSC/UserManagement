using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Services;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get recent audit logs
        /// </summary>
        /// <param name="count">Number of logs to retrieve (default: 100, max: 1000)</param>
        /// <returns>List of recent audit logs</returns>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetRecentAuditLogs([FromQuery] int count = 100)
        {
            _logger.LogInformation("API: GetRecentAuditLogs endpoint called with count: {Count}", count);

            if (count <= 0 || count > 1000)
            {
                _logger.LogWarning("API: GetRecentAuditLogs called with invalid count: {Count}", count);
                return BadRequest(ApiResponse<List<AuditLogDto>>.ErrorResult("Count must be between 1 and 1000"));
            }

            var result = await _auditLogService.GetRecentAuditLogsAsync(count);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get audit logs for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of audit logs for the user</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAuditLogsByUserId(int userId)
        {
            _logger.LogInformation("API: GetAuditLogsByUserId endpoint called for user ID: {UserId}", userId);

            var result = await _auditLogService.GetAuditLogsByUserIdAsync(userId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get audit logs by action type
        /// </summary>
        /// <param name="action">Action type (e.g., "User Created", "User Updated")</param>
        /// <returns>List of audit logs for the specified action</returns>
        [HttpGet("action/{action}")]
        [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAuditLogsByAction(string action)
        {
            _logger.LogInformation("API: GetAuditLogsByAction endpoint called for action: {Action}", action);

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogWarning("API: GetAuditLogsByAction called with empty action");
                return BadRequest(ApiResponse<List<AuditLogDto>>.ErrorResult("Action cannot be empty"));
            }

            var result = await _auditLogService.GetAuditLogsByActionAsync(action);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Manually log an action (for administrative purposes)
        /// </summary>
        /// <param name="logRequest">Log request details</param>
        /// <returns>Success status</returns>
        [HttpPost("log")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> LogAction([FromBody] ManualLogRequest logRequest)
        {
            _logger.LogInformation("API: LogAction endpoint called for action: {Action}", logRequest.Action);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: LogAction called with invalid model state");
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid input data"));
            }

            try
            {
                await _auditLogService.LogActionAsync(logRequest.UserId, logRequest.Action, logRequest.Metadata);

                var result = ApiResponse<bool>.SuccessResult(true, "Action logged successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error in LogAction endpoint");
                var result = ApiResponse<bool>.ErrorResult("Error logging action");
                return StatusCode(500, result);
            }
        }
    }

    /// <summary>
    /// Request model for manually logging actions
    /// </summary>
    public class ManualLogRequest
    {
        /// <summary>
        /// User ID (optional)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Action description
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata (optional)
        /// </summary>
        public string? Metadata { get; set; }
    }
}