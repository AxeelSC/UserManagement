using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.Services;

namespace UserManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        /// <returns>List of roles</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAllRoles()
        {
            _logger.LogInformation("API: GetAllRoles endpoint called");

            var result = await _roleService.GetAllRolesAsync();

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get role by ID
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>Role details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(int id)
        {
            _logger.LogInformation("API: GetRole endpoint called with ID: {RoleId}", id);

            var result = await _roleService.GetRoleByIdAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Role not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get role by name
        /// </summary>
        /// <param name="name">Role name</param>
        /// <returns>Role details</returns>
        [HttpGet("by-name/{name}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleByName(string name)
        {
            _logger.LogInformation("API: GetRoleByName endpoint called with name: {RoleName}", name);

            var result = await _roleService.GetRoleByNameAsync(name);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Role not found")
                return NotFound(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Get roles assigned to a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of roles assigned to the user</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRolesByUserId(int userId)
        {
            _logger.LogInformation("API: GetRolesByUserId endpoint called for user ID: {UserId}", userId);

            var result = await _roleService.GetRolesByUserIdAsync(userId);

            if (result.Success)
                return Ok(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Create a new role
        /// </summary>
        /// <param name="createRoleDto">Role creation details</param>
        /// <returns>Created role</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            _logger.LogInformation("API: CreateRole endpoint called for role: {RoleName}", createRoleDto.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: CreateRole called with invalid model state");
                return BadRequest(ApiResponse<RoleDto>.ErrorResult("Invalid input data"));
            }

            var result = await _roleService.CreateRoleAsync(createRoleDto);

            if (result.Success)
                return CreatedAtAction(nameof(GetRole), new { id = result.Data!.Id }, result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Update an existing role
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <param name="updateRoleDto">Role update details</param>
        /// <returns>Updated role</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
        {
            _logger.LogInformation("API: UpdateRole endpoint called for ID: {RoleId}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: UpdateRole called with invalid model state for ID: {RoleId}", id);
                return BadRequest(ApiResponse<RoleDto>.ErrorResult("Invalid input data"));
            }

            var result = await _roleService.UpdateRoleAsync(id, updateRoleDto);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Role not found")
                return NotFound(result);

            if (result.Message.Contains("already exists"))
                return BadRequest(result);

            return StatusCode(500, result);
        }

        /// <summary>
        /// Delete a role
        /// </summary>
        /// <param name="id">Role ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(int id)
        {
            _logger.LogInformation("API: DeleteRole endpoint called for ID: {RoleId}", id);

            var result = await _roleService.DeleteRoleAsync(id);

            if (result.Success)
                return Ok(result);

            if (result.Message == "Role not found")
                return NotFound(result);

            if (result.Message.Contains("assigned to users"))
                return BadRequest(result);

            return StatusCode(500, result);
        }
    }
}