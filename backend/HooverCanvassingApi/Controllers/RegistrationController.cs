using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationController> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<Volunteer> _userManager;
        private readonly IPasswordHasher<Volunteer> _passwordHasher;

        public RegistrationController(
            ApplicationDbContext context, 
            ILogger<RegistrationController> logger, 
            IEmailService emailService, 
            UserManager<Volunteer> userManager,
            IPasswordHasher<Volunteer> passwordHasher)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }

        // Send invitation email (Admin/SuperAdmin only)
        [HttpPost("send-invitation")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> SendInvitation([FromBody] SendInvitationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                _logger.LogInformation("Invitation request from {UserId} for {Email} as {Role}", 
                    currentUserId, request.Email, request.Role);

                // Validate role permissions
                if (currentUserRole == "Admin" && request.Role == "Admin")
                {
                    return Forbid("Admins cannot invite other admins");
                }
                if (currentUserRole == "Admin" && request.Role == "SuperAdmin")
                {
                    return Forbid("Admins cannot invite super admins");
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "User with this email already exists" });
                }

                // Check if there's already a pending invitation
                var existingToken = await _context.InvitationTokens
                    .FirstOrDefaultAsync(t => t.Email == request.Email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);
                
                if (existingToken != null)
                {
                    return BadRequest(new { error = "An active invitation already exists for this email" });
                }

                // Generate secure token
                var token = GenerateSecureToken();
                
                // Create invitation token
                var invitation = new InvitationToken
                {
                    Email = request.Email,
                    Role = Enum.Parse<VolunteerRole>(request.Role),
                    Token = token,
                    CreatedByUserId = currentUserId
                };

                _context.InvitationTokens.Add(invitation);
                await _context.SaveChangesAsync();

                // Get inviter name for email
                var inviter = await _userManager.FindByIdAsync(currentUserId!);
                var inviterName = $"{inviter?.FirstName} {inviter?.LastName}";
                
                // Generate registration URL
                var registrationUrl = $"https://t4h-canvas-2uwxt.ondigitalocean.app/complete-registration?token={token}";
                
                // Send invitation email
                var emailSent = await _emailService.SendInvitationEmailAsync(
                    request.Email, inviterName, registrationUrl, request.Role);

                if (!emailSent)
                {
                    _logger.LogError("Failed to send invitation email to {Email}", request.Email);
                    return StatusCode(500, new { error = "Failed to send invitation email" });
                }

                _logger.LogInformation("Invitation sent successfully to {Email}", request.Email);
                
                return Ok(new { 
                    message = "Invitation sent successfully", 
                    email = request.Email,
                    role = request.Role,
                    expiresAt = invitation.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation to {Email}", request.Email);
                return StatusCode(500, new { error = "Failed to send invitation" });
            }
        }

        // Complete registration from invitation token
        [HttpPost("complete-invitation")]
        public async Task<ActionResult> CompleteInvitation([FromBody] CompleteInvitationRequest request)
        {
            try
            {
                _logger.LogInformation("Completing invitation with token {Token}", request.Token);

                // Find and validate token
                var invitation = await _context.InvitationTokens
                    .Include(i => i.CreatedBy)
                    .FirstOrDefaultAsync(t => t.Token == request.Token);

                if (invitation == null)
                {
                    return BadRequest(new { error = "Invalid invitation token" });
                }

                if (invitation.IsUsed)
                {
                    return BadRequest(new { error = "Invitation token has already been used" });
                }

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest(new { error = "Invitation token has expired" });
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "User with this email already exists" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
                    string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { error = "All fields are required" });
                }

                // Create new user
                var newUser = new Volunteer
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = invitation.Email,
                    UserName = invitation.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = invitation.Role,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(newUser, request.Password);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { error = $"Failed to create user: {errors}" });
                }

                // Add user to role
                await _userManager.AddToRoleAsync(newUser, invitation.Role.ToString());

                // Mark invitation as used
                invitation.IsUsed = true;
                invitation.UsedAt = DateTime.UtcNow;
                invitation.CompletedByUserId = newUser.Id;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} completed registration from invitation", invitation.Email);

                return Ok(new { 
                    message = "Registration completed successfully", 
                    userId = newUser.Id,
                    email = newUser.Email,
                    role = newUser.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing invitation registration");
                return StatusCode(500, new { error = "Failed to complete registration" });
            }
        }

        // Self-registration (public endpoint)
        [HttpPost("self-register")]
        public async Task<ActionResult> SelfRegister([FromBody] SelfRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("Self-registration request from {Email}", request.Email);

                // Validate required fields
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
                    string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.PhoneNumber) ||
                    string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { error = "All fields are required" });
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "User with this email already exists" });
                }

                // Check if there's already a pending registration
                var existingPending = await _context.PendingVolunteers
                    .FirstOrDefaultAsync(p => p.Email == request.Email);
                
                if (existingPending != null)
                {
                    return BadRequest(new { error = "A registration with this email is already pending approval" });
                }

                // Create pending volunteer record
                var pendingVolunteer = new PendingVolunteer
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    HashedPassword = _passwordHasher.HashPassword(new Volunteer(), request.Password),
                    RequestedRole = VolunteerRole.Volunteer // Self-registrations default to Volunteer
                };

                _context.PendingVolunteers.Add(pendingVolunteer);
                await _context.SaveChangesAsync();

                // Send notification to all admins and super admins
                await NotifyAdminsOfPendingRegistration(pendingVolunteer);

                _logger.LogInformation("Self-registration created for {Email}, awaiting approval", request.Email);

                return Ok(new { 
                    message = "Registration submitted successfully. You will receive an email once your application is reviewed.",
                    email = request.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing self-registration for {Email}", request.Email);
                return StatusCode(500, new { error = "Failed to process registration" });
            }
        }

        // Get invitation details (for registration form pre-fill)
        [HttpGet("invitation/{token}")]
        public async Task<ActionResult> GetInvitationDetails(string token)
        {
            try
            {
                var invitation = await _context.InvitationTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (invitation == null)
                {
                    return NotFound(new { error = "Invalid invitation token" });
                }

                if (invitation.IsUsed)
                {
                    return BadRequest(new { error = "Invitation token has already been used" });
                }

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest(new { error = "Invitation token has expired" });
                }

                return Ok(new {
                    email = invitation.Email,
                    role = invitation.Role.ToString(),
                    expiresAt = invitation.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invitation details for token {Token}", token);
                return StatusCode(500, new { error = "Failed to get invitation details" });
            }
        }

        private async Task NotifyAdminsOfPendingRegistration(PendingVolunteer pendingVolunteer)
        {
            try
            {
                // Get all admins and super admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                var allAdmins = admins.Concat(superAdmins).Where(u => u.IsActive).ToList();

                if (!allAdmins.Any())
                {
                    _logger.LogWarning("No active admins found to notify about pending registration {Email}", pendingVolunteer.Email);
                    return;
                }

                var notificationData = new PendingRegistrationData
                {
                    FirstName = pendingVolunteer.FirstName,
                    LastName = pendingVolunteer.LastName,
                    Email = pendingVolunteer.Email,
                    PhoneNumber = pendingVolunteer.PhoneNumber,
                    RequestedRole = pendingVolunteer.RequestedRole.ToString(),
                    RegistrationTime = pendingVolunteer.CreatedAt,
                    PendingVolunteerId = pendingVolunteer.Id
                };

                // Send notifications to all admins
                var notificationTasks = allAdmins.Select(admin => 
                    _emailService.SendRegistrationApprovalNotificationAsync(admin.Email!, notificationData)
                ).ToArray();

                var results = await Task.WhenAll(notificationTasks);
                var successCount = results.Count(r => r);
                
                _logger.LogInformation("Sent {SuccessCount} admin notifications for pending registration {Email}", 
                    successCount, pendingVolunteer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify admins about pending registration {Email}", pendingVolunteer.Email);
            }
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }

    public class SendInvitationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Volunteer"; // Volunteer, Admin, SuperAdmin
    }

    public class CompleteInvitationRequest
    {
        public string Token { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SelfRegistrationRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}