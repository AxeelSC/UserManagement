using Microsoft.AspNetCore.Authorization;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.RoleManagement;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Infrastructure.Authorization;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class RoleManagementController : ControllerBase
    {
        private readonly IRoleManagementService _roleManagementService;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(IRoleManagementService roleManagementService, ILogger<RoleManagementController> logger)
        {
            _roleManagementService = roleManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Promote user to manager (Admin only)
        /// </summary>
        [HttpPost("promote-to-manager")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> PromoteToManager([FromBody] PromoteToManagerDto dto)
        {
            _logger.LogInformation("API: PromoteToManager called by admin: {Username}", User.GetUsername());

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid input data"));
            }

            var adminId = User.GetUserId();
            var result = await _roleManagementService.PromoteToManagerAsync(adminId, dto.UserId, dto.TeamId);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Demote manager to user (Admin only)
        /// </summary>
        [HttpPost("demote-manager/{managerId}")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DemoteManager(int managerId)
        {
            _logger.LogInformation("API: DemoteManager({ManagerId}) called by admin: {Username}", managerId, User.GetUsername());

            var adminId = User.GetUserId();
            var result = await _roleManagementService.DemoteManagerAsync(adminId, managerId);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Change user role (Manager for team members, Admin for any user)
        /// </summary>
        [HttpPost("change-role/{userId}")]
        [RequireRole("Manager", "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangeUserRole(int userId, [FromBody] ChangeRoleDto dto)
        {
            _logger.LogInformation("API: ChangeUserRole({UserId}) to {NewRole} called by: {Username}", userId, dto.NewRole, User.GetUsername());

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid input data"));
            }

            var requestingUserId = User.GetUserId();
            var result = await _roleManagementService.ChangeUserRoleAsync(requestingUserId, userId, dto.NewRole);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get available roles for a user (what roles the current user can assign to the target user)
        /// </summary>
        [HttpGet("available-roles/{userId}")]
        [RequireRole("Manager", "Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetAvailableRoles(int userId)
        {
            _logger.LogInformation("API: GetAvailableRoles({UserId}) called by: {Username}", userId, User.GetUsername());

            var requestingUserId = User.GetUserId();
            var result = await _roleManagementService.GetAvailableRolesForUserAsync(requestingUserId, userId);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}