using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Services;

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
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<Volunteer> userManager,
            SignInManager<Volunteer> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthUserDto>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}, password length: {PasswordLength}", 
                    request.Email, request.Password?.Length ?? 0);

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Login failed - missing email or password for {Email}", request.Email);
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Email and password are required"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Login failed - user not found or inactive for {Email}", request.Email);
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Invalid email or password"
                    });
                }

                _logger.LogInformation("User found for {Email}, checking password. User active: {IsActive}", 
                    request.Email, user.IsActive);

                // Enhanced debugging for password validation
                _logger.LogInformation("User found - PasswordHash null: {IsNull}, Hash length: {Length}, Email confirmed: {EmailConfirmed}, Lockout enabled: {LockoutEnabled}", 
                    user.PasswordHash == null, user.PasswordHash?.Length ?? 0, user.EmailConfirmed, user.LockoutEnabled);
                
                // Try direct password check first
                var directPasswordCheck = await _userManager.CheckPasswordAsync(user, request.Password);
                _logger.LogInformation("Direct password check for {Email}: {Success}", request.Email, directPasswordCheck);
                
                // Also check password hash directly for debugging
                if (!directPasswordCheck)
                {
                    var hasher = _userManager.PasswordHasher;
                    var hashCheckResult = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                    _logger.LogWarning("Direct hash check result for {Email}: {Result}", request.Email, hashCheckResult);
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                _logger.LogInformation("SignInManager check for {Email}: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, IsNotAllowed={IsNotAllowed}, RequiresTwoFactor={RequiresTwoFactor}", 
                    request.Email, result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Login failed for {Email} - SignInManager check failed", request.Email);
                    return BadRequest(new ApiResponse<AuthUserDto>
                    {
                        Success = false,
                        Error = "Invalid email or password"
                    });
                }

                // Update login tracking
                user.LoginCount++;
                user.LastLoginAt = DateTime.UtcNow;
                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = token,
                    AvatarUrl = GetGravatarUrl(user.Email!)
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
                    PhoneNumber = request.PhoneNumber,
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

                var token = GenerateJwtToken(user);
                var authUser = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString().ToLower(),
                    Token = token,
                    AvatarUrl = GetGravatarUrl(user.Email)
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

                var token = GenerateJwtToken(user);
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
                    PhoneNumber = request.PhoneNumber,
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

        [HttpGet("avatar-info")]
        [Authorize]
        public async Task<ActionResult> GetAvatarInfo()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new
                {
                    email = user.Email,
                    avatarUrl = GetGravatarUrl(user.Email!),
                    gravatarInfo = new
                    {
                        message = "To change your avatar, create or update your Gravatar account at gravatar.com using your email address.",
                        gravatarUrl = "https://gravatar.com",
                        emailUsed = user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting avatar info for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { error = "Failed to get avatar info" });
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

        private string GetGravatarUrl(string email, int size = 80)
        {
            using var md5 = MD5.Create();
            var emailBytes = Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant());
            var hashBytes = md5.ComputeHash(emailBytes);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"https://www.gravatar.com/avatar/{hash}?s={size}&d=identicon&r=pg";
        }

        private string GenerateJwtToken(Volunteer user)
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

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Email is required"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                
                // Always return success to prevent email enumeration attacks
                var response = new ApiResponse<object>
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent."
                };

                if (user != null && user.IsActive)
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
                    var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(user.Email!)}";

                    var emailSent = await _emailService.SendPasswordResetEmailAsync(
                        user.Email!, 
                        user.FirstName, 
                        resetToken, 
                        resetUrl
                    );

                    if (emailSent)
                    {
                        _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                    }
                    else
                    {
                        _logger.LogError("Failed to send password reset email to {Email}", request.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("Password reset requested for non-existent or inactive user: {Email}", request.Email);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request for {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "An error occurred while processing your request"
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] PasswordResetRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Email, token, and new password are required"
                    });
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Invalid reset token or user not found"
                    });
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user {Email}", request.Email);
                    
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Password has been reset successfully"
                    });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password reset failed for user {Email}: {Errors}", request.Email, errors);
                    
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Error = "Failed to reset password. " + errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Error = "An error occurred while resetting your password"
                });
            }
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
        public string? PhoneNumber { get; set; }
    }

    public class AuthUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class PasswordResetRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
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