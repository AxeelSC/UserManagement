using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagementSystem.Application.DTOs;
using UserManagementSystem.Application.DTOs.Auth;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Application.Services;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", request.Username);

                // Find user by username
                var user = await _unitOfWork.Users.GetUserWithRolesAsync(
                    await _unitOfWork.Users.GetByUsernameAsync(request.Username) is User u ? u.Id : 0);

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Username}", request.Username);
                    return ApiResponse<LoginResponseDto>.ErrorResult("Invalid username or password");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed - user is inactive: {Username}", request.Username);
                    return ApiResponse<LoginResponseDto>.ErrorResult("Account is deactivated");
                }

                // Verify password
                if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed - invalid password for user: {Username}", request.Username);

                    // Log failed login attempt
                    await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                    {
                        UserId = user.Id,
                        Action = "Failed Login Attempt",
                        Metadata = $"Username: {request.Username}",
                        Timestamp = DateTime.UtcNow
                    });
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResponse<LoginResponseDto>.ErrorResult("Invalid username or password");
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"]!));

                // Log successful login
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = user.Id,
                    Action = "User Login",
                    Metadata = $"Username: {user.Username}",
                    Timestamp = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();

                var response = new LoginResponseDto
                {
                    Token = token,
                    User = MapToUserDto(user),
                    ExpiresAt = expiresAt,
                    TokenType = "Bearer"
                };

                _logger.LogInformation("Login successful for user: {Username}", user.Username);
                return ApiResponse<LoginResponseDto>.SuccessResult(response, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
                return ApiResponse<LoginResponseDto>.ErrorResult("An error occurred during login");
            }
        }

        public async Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                _logger.LogInformation("Registration attempt for username: {Username}", request.Username);

                // Validation
                if (!await _unitOfWork.Users.IsUsernameUniqueAsync(request.Username))
                {
                    _logger.LogWarning("Registration failed - duplicate username: {Username}", request.Username);
                    return ApiResponse<UserDto>.ErrorResult("Username already exists");
                }

                if (!await _unitOfWork.Users.IsEmailUniqueAsync(request.Email))
                {
                    _logger.LogWarning("Registration failed - duplicate email: {Email}", request.Email);
                    return ApiResponse<UserDto>.ErrorResult("Email already exists");
                }

                // Password strength validation
                if (!_passwordService.IsPasswordStrong(request.Password))
                {
                    _logger.LogWarning("Registration failed - weak password for user: {Username}", request.Username);
                    return ApiResponse<UserDto>.ErrorResult("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
                }

                // Create user entity
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    TeamId = null // Users start without a team
                };

                // Add user to database
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Assign default "User" role (ID = 3)
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = 3, // User role
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserRoles.AddAsync(userRole);

                // Log the registration
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = user.Id,
                    Action = "User Registered",
                    Metadata = $"Username: {user.Username}, Email: {user.Email}",
                    Timestamp = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                // Get user with roles for response
                var userWithRoles = await _unitOfWork.Users.GetUserWithRolesAsync(user.Id);
                var userDto = MapToUserDto(userWithRoles!);

                _logger.LogInformation("Registration successful for user: {Username}", user.Username);
                return ApiResponse<UserDto>.SuccessResult(userDto, "Registration successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
                return ApiResponse<UserDto>.ErrorResult("An error occurred during registration");
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("teamId", user.TeamId?.ToString() ?? ""),
                new Claim("isActive", user.IsActive.ToString())
            };

            // Add role claims
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> GetUserByTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);

                return await _unitOfWork.Users.GetUserWithRolesAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Roles = user.UserRoles?.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                }).ToList() ?? new List<RoleDto>()
            };
        }
    }
}