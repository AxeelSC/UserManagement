using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Infrastructure.Authorization;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all endpoints
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin and Manager only)
        /// </summary>
        [HttpGet]
        [RequireRole("Admin", "Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<UserSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetAllUsers()
        {
            _logger.LogInformation("API: GetAllUsers called by user: {Username}", User.GetUsername());

            var result = await _userService.GetAllUsersAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get active users only (Admin and Manager only)
        /// </summary>
        [HttpGet("active")]
        [RequireRole("Admin", "Manager")]
        [ProducesResponseType(typeof(ApiResponse<List<UserSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetActiveUsers()
        {
            _logger.LogInformation("API: GetActiveUsers called by user: {Username}", User.GetUsername());

            var result = await _userService.GetActiveUsersAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get user by ID (Admin, Manager for team members, or own profile)
        /// </summary>
        [HttpGet("{id}")]
        [RequireTeamAccess]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
        {
            _logger.LogInformation("API: GetUser({UserId}) called by user: {Username}", id, User.GetUsername());

            var result = await _userService.GetUserByIdAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }


        /// <summary>
        /// Update user (Admin, Manager for team members, or own profile)
        /// </summary>
        [HttpPut("{id}")]
        [RequireTeamAccess]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            _logger.LogInformation("API: UpdateUser({UserId}) called by user: {Username}", id, User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: UpdateUser called with invalid model state");
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid input data"));
            }

            var result = await _userService.UpdateUserAsync(id, updateUserDto);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
        {
            _logger.LogInformation("API: DeleteUser({UserId}) called by admin: {Username}", id, User.GetUsername());

            var result = await _userService.DeleteUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Change user password (Admin, Manager for team members, or own profile)
        /// </summary>
        [HttpPost("{id}/change-password")]
        [RequireTeamAccess]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            _logger.LogInformation("API: ChangePassword called for user {UserId} by {Username}", id, User.GetUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: ChangePassword called with invalid model state");
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid input data"));
            }

            var result = await _userService.ChangePasswordAsync(id, changePasswordDto);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            if (result.Message == "Current password is incorrect")
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Activate user (Admin only)
        /// </summary>
        [HttpPost("{id}/activate")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> ActivateUser(int id)
        {
            _logger.LogInformation("API: ActivateUser({UserId}) called by admin: {Username}", id, User.GetUsername());

            var result = await _userService.ActivateUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [RequireRole("Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(int id)
        {
            _logger.LogInformation("API: DeactivateUser({UserId}) called by admin: {Username}", id, User.GetUsername());

            var result = await _userService.DeactivateUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }
    }
}