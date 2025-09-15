using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Auth;
using UserManagementSystem.Application.Services;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Test JWT authentication (for debugging)
        /// </summary>
        [HttpGet("test")]
        [Authorize]
        public ActionResult<string> TestAuth()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;
            var roles = string.Join(", ", User.FindAll(ClaimTypes.Role).Select(c => c.Value));

            return Ok($"Authenticated! UserId: {userId}, Username: {username}, Roles: {roles}");
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("API: Login endpoint called for username: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Login called with invalid model state");
                return BadRequest(ApiResponse<LoginResponseDto>.ErrorResult("Invalid input data"));
            }

            var result = await _authService.LoginAsync(request);

            if (result.Success)
                return Ok(result);

            if (result.Message.Contains("Invalid") || result.Message.Contains("deactivated"))
                return Unauthorized(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// User registration
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequestDto request)
        {
            _logger.LogInformation("API: Register endpoint called for username: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Register called with invalid model state");
                return BadRequest(ApiResponse<UserDto>.ErrorResult("Invalid input data"));
            }

            var result = await _authService.RegisterAsync(request);

            if (result.Success)
                return CreatedAtAction(nameof(Login), result);

            if (result.Message.Contains("already exists") || result.Message.Contains("Password must"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get current user information from token
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResult("Invalid token"));
                }

                var userId = int.Parse(userIdClaim.Value);
                _logger.LogInformation("API: GetCurrentUser called for user ID: {UserId}", userId);

                // You can use your existing UserService here
                // For now, let's create a simple response
                var response = new UserDto
                {
                    Id = userId,
                    Username = User.Identity!.Name!,
                    Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
                    IsActive = bool.Parse(User.FindFirst("isActive")?.Value ?? "true"),
                    Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                        .Select(r => new RoleDto { Name = r.Value })
                        .ToList()
                };

                return Ok(ApiResponse<UserDto>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred"));
            }
        }

        /// <summary>
        /// Logout (client-side token invalidation)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;

            _logger.LogInformation("API: Logout endpoint called for user: {Username}", username);

            // Note: With JWT, logout is typically handled client-side by discarding the token
            // For server-side logout, you'd need to maintain a token blacklist

            var result = ApiResponse<bool>.SuccessResult(true, "Logged out successfully. Please discard your token.");
            return Ok(result);
        }
    }
}