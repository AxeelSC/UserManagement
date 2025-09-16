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
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Get all teams (All authenticated users can view teams)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<TeamSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<TeamSummaryDto>>>> GetAllTeams()
        {
            _logger.LogInformation("API: GetAllTeams called by user: {Username}", User.GetUsername());

            var result = await _teamService.GetAllTeamsAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get team by ID (All authenticated users can view team details)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamDto>>> GetTeam(int id)
        {
            _logger.LogInformation("API: GetTeam({TeamId}) called by user: {Username}", id, User.GetUsername());

            var result = await _teamService.GetTeamByIdAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Team not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get team by name (All authenticated users can view team details)
        /// </summary>
        [HttpGet("by-name/{name}")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamDto>>> GetTeamByName(string name)
        {
            _logger.LogInformation("API: GetTeamByName({TeamName}) called by user: {Username}", name, User.GetUsername());

            var result = await _teamService.GetTeamByNameAsync(name);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Team not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Create a new team (Admin only)
        /// </summary>
        [HttpPost]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamDto>>> CreateTeam([FromBody] CreateTeamDto createTeamDto)
        {
            _logger.LogInformation("API: CreateTeam called by admin: {Username}", User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: CreateTeam called with invalid model state");
                return BadRequest(ApiResponse<TeamDto>.ErrorResult("Invalid input data"));
            }

            var result = await _teamService.CreateTeamAsync(createTeamDto);

            if (result.Success)
                return CreatedAtAction(nameof(GetTeam), new { id = result.Data!.Id }, result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Update a team (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<TeamDto>>> UpdateTeam(int id, [FromBody] UpdateTeamDto updateTeamDto)
        {
            _logger.LogInformation("API: UpdateTeam({TeamId}) called by admin: {Username}", id, User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: UpdateTeam called with invalid model state");
                return BadRequest(ApiResponse<TeamDto>.ErrorResult("Invalid input data"));
            }

            var result = await _teamService.UpdateTeamAsync(id, updateTeamDto);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Team not found")
                return NotFound(result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Delete a team (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTeam(int id)
        {
            _logger.LogInformation("API: DeleteTeam({TeamId}) called by admin: {Username}", id, User.GetUsername());

            var result = await _teamService.DeleteTeamAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Team not found")
                return NotFound(result);

            if (result.Message.Contains("has members"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get teams managed by current user (Manager only)
        /// </summary>
        [HttpGet("managed")]
        [RequireRole("Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<TeamSummaryDto>>>> GetManagedTeams()
        {
            _logger.LogInformation("API: GetManagedTeams called by manager: {Username}", User.GetUsername());

            var managerId = User.GetUserId();
            var result = await _teamService.GetTeamsForManagerAsync(managerId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Assign a manager to a team (Admin only)
        /// </summary>
        [HttpPost("{teamId}/assign-manager/{userId}")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> AssignManager(int teamId, int userId)
        {
            _logger.LogInformation("API: AssignManager - Team {TeamId}, User {UserId} called by admin: {Username}",
                teamId, userId, User.GetUsername());

            var result = await _teamService.AssignManagerAsync(teamId, userId);

            if (result.Success)
                return Ok(result);

            if (result.Message.Contains("not found") || result.Message.Contains("already has"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Remove manager from a team (Admin only)
        /// </summary>
        [HttpDelete("{teamId}/remove-manager")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveManager(int teamId)
        {
            _logger.LogInformation("API: RemoveManager from team {TeamId} called by admin: {Username}",
                teamId, User.GetUsername());

            var result = await _teamService.RemoveManagerAsync(teamId);

            if (result.Success)
                return Ok(result);

            if (result.Message.Contains("No manager"))
                return BadRequest(result);

            return StatusCode(500, result);
        }
    }
}