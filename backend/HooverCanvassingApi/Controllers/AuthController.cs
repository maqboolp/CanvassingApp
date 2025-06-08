using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Volunteer> _userManager;
        private readonly SignInManager<Volunteer> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<Volunteer> userManager,
            SignInManager<Volunteer> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Email and password are required"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Invalid email or password"
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Invalid email or password"
                    });
                }

                var token = await GenerateJwtToken(user);
                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = token
                };

                _logger.LogInformation("User {Email} logged in successfully", request.Email);

                return Ok(new ApiResponse<AuthUserDto>
                {
                    Success = true,
                    Data = authUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                return StatusCode(500, new ApiResponse<AuthUserDto>
                {
                    Success = false,
                    Error = "Login failed"
                });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || 
                    string.IsNullOrEmpty(request.Password) || 
                    string.IsNullOrEmpty(request.FirstName) || 
                    string.IsNullOrEmpty(request.LastName))
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "All fields are required"
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "User with this email already exists"
                    });
                }

                var user = new Volunteer
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = VolunteerRole.Volunteer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true // Auto-confirm for simplicity
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = $"Registration failed: {errors}"
                    });
                }

                var token = await GenerateJwtToken(user);
                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = token
                };

                _logger.LogInformation("New user {Email} registered successfully", request.Email);

                return Ok(new ApiResponse<AuthUserDto>
                {
                    Success = true,
                    Data = authUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Email}", request.Email);
                return StatusCode(500, new ApiResponse<AuthUserDto>
                {
                    Success = false,
                    Error = "Registration failed"
                });
            }
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> RefreshToken()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Invalid token"
                    });
                }

                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "User not found or inactive"
                    });
                }

                var token = await GenerateJwtToken(user);
                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = token
                };

                return Ok(new ApiResponse<AuthUserDto>
                {
                    Success = true,
                    Data = authUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponse<AuthUserDto>
                {
                    Success = false,
                    Error = "Token refresh failed"
                });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> GetCurrentUser()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "User not found"
                    });
                }

                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = string.Empty // Don't send token in this endpoint
                };

                return Ok(new ApiResponse<AuthUserDto>
                {
                    Success = true,
                    Data = authUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse<AuthUserDto>
                {
                    Success = false,
                    Error = "Failed to get user information"
                });
            }
        }

        [HttpPost("create-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> CreateAdmin([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || 
                    string.IsNullOrEmpty(request.Password) || 
                    string.IsNullOrEmpty(request.FirstName) || 
                    string.IsNullOrEmpty(request.LastName))
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "All fields are required"
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "User with this email already exists"
                    });
                }

                var user = new Volunteer
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = VolunteerRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = $"Admin creation failed: {errors}"
                    });
                }

                _logger.LogInformation("Admin created successfully: {Email}", request.Email);

                return Ok(new ApiResponse<AuthUserDto>
                {
                    Success = true,
                    Message = "Admin created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin");
                return StatusCode(500, new ApiResponse<AuthUserDto>
                {
                    Success = false,
                    Error = "Admin creation failed"
                });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Error = "Invalid token"
                    });
                }

                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Error = "User not found or inactive"
                    });
                }

                // Verify current password
                var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Error = "Current password is incorrect"
                    });
                }

                // Change password
                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Error = $"Password change failed: {errors}"
                    });
                }

                _logger.LogInformation("Password changed successfully for user {Email}", user.Email);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Error = "Password change failed"
                });
            }
        }

        private async Task<string> GenerateJwtToken(Volunteer user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "480");

            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException("JWT secret not configured");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("firstName", user.FirstName),
                new("lastName", user.LastName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class AuthUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}