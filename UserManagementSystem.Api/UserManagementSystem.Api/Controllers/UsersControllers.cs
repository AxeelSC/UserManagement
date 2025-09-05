using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Services;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
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
        /// Get all users
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<UserSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetAllUsers()
        {
            _logger.LogInformation("API: GetAllUsers endpoint called");

            var result = await _userService.GetAllUsersAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get active users only
        /// </summary>
        /// <returns>List of active users</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponse<List<UserSummaryDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<UserSummaryDto>>>> GetActiveUsers()
        {
            _logger.LogInformation("API: GetActiveUsers endpoint called");

            var result = await _userService.GetActiveUsersAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
        {
            _logger.LogInformation("API: GetUser endpoint called with ID: {UserId}", id);

            var result = await _userService.GetUserByIdAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User details</returns>
        [HttpGet("by-username/{username}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByUsername(string username)
        {
            _logger.LogInformation("API: GetUserByUsername endpoint called with username: {Username}", username);

            var result = await _userService.GetUserByUsernameAsync(username);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>User details</returns>
        [HttpGet("by-email/{email}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(string email)
        {
            _logger.LogInformation("API: GetUserByEmail endpoint called with email: {Email}", email);

            var result = await _userService.GetUserByEmailAsync(email);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="createUserDto">User creation details</param>
        /// <returns>Created user</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            _logger.LogInformation("API: CreateUser endpoint called for username: {Username}", createUserDto.Username);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: CreateUser called with invalid model state");
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid input data"));
            }

            var result = await _userService.CreateUserAsync(createUserDto);

            if (result.Success)
                return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="updateUserDto">User update details</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            _logger.LogInformation("API: UpdateUser endpoint called for ID: {UserId}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: UpdateUser called with invalid model state for ID: {UserId}", id);
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
        /// Delete a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
        {
            _logger.LogInformation("API: DeleteUser endpoint called for ID: {UserId}", id);

            var result = await _userService.DeleteUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="changePasswordDto">Password change details</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            _logger.LogInformation("API: ChangePassword endpoint called for user ID: {UserId}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: ChangePassword called with invalid model state for ID: {UserId}", id);
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
        /// Activate a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> ActivateUser(int id)
        {
            _logger.LogInformation("API: ActivateUser endpoint called for ID: {UserId}", id);

            var result = await _userService.ActivateUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Deactivate a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(int id)
        {
            _logger.LogInformation("API: DeactivateUser endpoint called for ID: {UserId}", id);

            var result = await _userService.DeactivateUserAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "User not found")
                return NotFound(result);

            return StatusCode(500, result);
        }
    }
}