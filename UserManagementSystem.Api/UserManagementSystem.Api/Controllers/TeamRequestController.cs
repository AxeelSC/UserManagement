using Microsoft.AspNetCore.Authorization;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Teams;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Infrastructure.Authorization;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class TeamRequestsController : ControllerBase
    {
        private readonly ITeamRequestService _teamRequestService;
        private readonly ILogger<TeamRequestsController> _logger;

        public TeamRequestsController(ITeamRequestService teamRequestService, ILogger<TeamRequestsController> logger)
        {
            _teamRequestService = teamRequestService;
            _logger = logger;
        }

        /// <summary>
        /// Create a request to join a team (All authenticated users)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TeamRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamRequestDto>>> CreateRequest([FromBody] CreateTeamRequestDto createRequestDto)
        {
            _logger.LogInformation("API: CreateRequest called by user: {Username}", User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: CreateRequest called with invalid model state");
                return BadRequest(ApiResponse<TeamRequestDto>.ErrorResult("Invalid input data"));
            }

            var userId = User.GetUserId();
            var result = await _teamRequestService.CreateRequestAsync(userId, createRequestDto);

            if (result.Success)
                return Created($"/api/teamrequests/{result.Data!.Id}", result);

            if (result.Message.Contains("already") || result.Message.Contains("not found") || result.Message.Contains("no manager"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get current user's team requests
        /// </summary>
        [HttpGet("my-requests")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<TeamRequestDto>>>> GetMyRequests()
        {
            _logger.LogInformation("API: GetMyRequests called by user: {Username}", User.GetUsername());

            var userId = User.GetUserId();
            var result = await _teamRequestService.GetRequestsByUserAsync(userId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get team mailbox - pending requests for manager's team (Manager only)
        /// </summary>
        [HttpGet("mailbox")]
        [RequireRole("Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<TeamRequestDto>>>> GetTeamMailbox()
        {
            _logger.LogInformation("API: GetTeamMailbox called by manager: {Username}", User.GetUsername());

            var managerId = User.GetUserId();
            var result = await _teamRequestService.GetTeamMailboxAsync(managerId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Process a team request (approve or reject) - Manager only
        /// </summary>
        [HttpPost("{id}/process")]
        [RequireRole("Manager")]
        [ProducesResponseType(typeof(ApiResponse<TeamRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamRequestDto>>> ProcessRequest(int id, [FromBody] ProcessTeamRequestDto processDto)
        {
            _logger.LogInformation("API: ProcessRequest({RequestId}) called by manager: {Username}", id, User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: ProcessRequest called with invalid model state");
                return BadRequest(ApiResponse<TeamRequestDto>.ErrorResult("Invalid input data"));
            }

            var processedByUserId = User.GetUserId();
            var result = await _teamRequestService.ProcessRequestAsync(id, processedByUserId, processDto);

            if (result.Success)
                return Ok(result);

            if (result.Message.Contains("not found"))
                return NotFound(result);

            if (result.Message.Contains("already been processed"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Cancel a pending request (Users can cancel their own requests)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> CancelRequest(int id)
        {
            _logger.LogInformation("API: CancelRequest({RequestId}) called by user: {Username}", id, User.GetUsername());

            var userId = User.GetUserId();
            var result = await _teamRequestService.CancelRequestAsync(id, userId);

            if (result.Success)
                return Ok(result);

            if (result.Message.Contains("not found"))
                return NotFound(result);

            if (result.Message.Contains("can only cancel") || result.Message.Contains("Only pending"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get pending requests for a specific team (Admin and Manager of that team only)
        /// </summary>
        [HttpGet("team/{teamId}")]
        [RequireRole("Admin", "Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamRequestDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<TeamRequestDto>>>> GetRequestsForTeam(int teamId)
        {
            _logger.LogInformation("API: GetRequestsForTeam({TeamId}) called by user: {Username}", teamId, User.GetUsername());

            var result = await _teamRequestService.GetPendingRequestsForTeamAsync(teamId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }
    }
}