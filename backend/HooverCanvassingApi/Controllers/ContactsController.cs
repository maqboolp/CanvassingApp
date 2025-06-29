using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactsController> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<Volunteer> _userManager;
        private readonly IConfiguration _configuration;

        public ContactsController(ApplicationDbContext context, ILogger<ContactsController> logger, IEmailService emailService, UserManager<Volunteer> userManager, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpGet("test")]
        public async Task<ActionResult> TestEndpoint()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            
            _logger.LogInformation("=== CONTACTS API TEST ENDPOINT CALLED ===");
            _logger.LogInformation("Test endpoint called by user {UserId} ({UserEmail})", currentUserId, currentUserEmail);
            
            return Ok(new { 
                message = "Contacts API is working", 
                timestamp = DateTime.UtcNow,
                userId = currentUserId,
                userEmail = currentUserEmail
            });
        }

        [HttpGet("debug-superadmins")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DebugSuperAdmins()
        {
            try
            {
                _logger.LogInformation("=== DEBUGGING SUPER ADMINS ===");
                
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                _logger.LogInformation("Total super admins found: {Count}", superAdmins.Count);
                
                var adminDetails = superAdmins.Select(admin => {
                    var details = new {
                        Id = admin.Id,
                        Email = admin.Email,
                        FirstName = admin.FirstName,
                        LastName = admin.LastName,
                        IsActive = admin.IsActive,
                        EmailConfirmed = admin.EmailConfirmed,
                        HasValidEmail = !string.IsNullOrEmpty(admin.Email)
                    };
                    
                    _logger.LogInformation("Super Admin: {Email} - Active: {IsActive}, ValidEmail: {HasValidEmail}", 
                        admin.Email, admin.IsActive, !string.IsNullOrEmpty(admin.Email));
                    
                    return details;
                }).ToList();

                // Check how many would actually get emails
                var eligibleForEmails = superAdmins.Where(admin => 
                    admin.IsActive && !string.IsNullOrEmpty(admin.Email)
                ).ToList();
                
                _logger.LogInformation("Super admins eligible for emails: {EligibleCount} out of {TotalCount}", 
                    eligibleForEmails.Count, superAdmins.Count);

                return Ok(new {
                    message = "Super admin debug completed",
                    totalSuperAdmins = superAdmins.Count,
                    eligibleForEmails = eligibleForEmails.Count,
                    superAdmins = adminDetails,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging super admins");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                
                _logger.LogInformation("=== CONTACT CREATION STARTED ===");
                _logger.LogInformation("Contact creation request received from user {UserId} ({UserEmail}) for voter {VoterId}", 
                    currentUserId, currentUserEmail, request.VoterId);
                _logger.LogInformation("Contact request details: Status={Status}, Support={Support}, Notes={Notes}", 
                    request.Status, request.VoterSupport, request.Notes);
                
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("Contact creation failed: Unauthorized user");
                    return Unauthorized();
                }

                // Validate voter exists
                _logger.LogInformation("Looking up voter with ID: {VoterId}", request.VoterId);
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.LalVoterId == request.VoterId);

                if (voter == null)
                {
                    _logger.LogWarning("Contact creation failed: Voter {VoterId} not found", request.VoterId);
                    return NotFound(new { error = "Voter not found" });
                }
                
                _logger.LogInformation("Voter found: {VoterName} at {VoterAddress}", 
                    $"{voter.FirstName} {voter.LastName}", voter.AddressLine);

                // Check proximity if enabled
                var proximityConfig = _configuration.GetSection("ContactProximity");
                var enableProximityCheck = proximityConfig.GetValue<bool>("EnableProximityCheck", true);
                var maxDistanceMeters = proximityConfig.GetValue<double>("MaxDistanceMeters", 100);
                var bypassRoles = proximityConfig.GetSection("BypassForRoles").Get<string[]>() ?? new string[] { "SuperAdmin" };
                
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var shouldCheckProximity = enableProximityCheck && !bypassRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
                
                if (shouldCheckProximity)
                {
                    // Validate that location was provided
                    if (request.Location == null)
                    {
                        _logger.LogWarning("Contact creation failed: Location not provided for user {UserId}", currentUserId);
                        return BadRequest(new { error = "Location is required to create a contact", requiresLocation = true });
                    }
                    
                    // Validate that voter has coordinates
                    if (!voter.Latitude.HasValue || !voter.Longitude.HasValue)
                    {
                        _logger.LogWarning("Contact creation failed: Voter {VoterId} has no coordinates", request.VoterId);
                        return BadRequest(new { error = "Cannot verify proximity - voter location not available" });
                    }
                    
                    // Calculate distance
                    var distance = CalculateDistance(
                        request.Location.Latitude,
                        request.Location.Longitude,
                        voter.Latitude.Value,
                        voter.Longitude.Value
                    );
                    
                    _logger.LogInformation("Proximity check: User is {Distance}m from voter (max allowed: {MaxDistance}m)", 
                        Math.Round(distance), maxDistanceMeters);
                    
                    if (distance > maxDistanceMeters)
                    {
                        _logger.LogWarning("Contact creation failed: User {UserId} is too far ({Distance}m) from voter {VoterId}", 
                            currentUserId, Math.Round(distance), request.VoterId);
                        return BadRequest(new { 
                            error = $"You must be within {maxDistanceMeters} meters of the voter to create a contact", 
                            currentDistance = Math.Round(distance),
                            maxDistance = maxDistanceMeters,
                            proximityRequired = true
                        });
                    }
                }

                // Parse contact status
                if (!Enum.TryParse<ContactStatus>(request.Status, true, out var contactStatus))
                {
                    return BadRequest(new { error = "Invalid contact status" });
                }

                // Parse voter support if provided
                VoterSupport? voterSupport = null;
                if (!string.IsNullOrEmpty(request.VoterSupport))
                {
                    if (!Enum.TryParse<VoterSupport>(request.VoterSupport, true, out var supportLevel))
                    {
                        return BadRequest(new { error = "Invalid voter support level" });
                    }
                    voterSupport = supportLevel;
                }

                // Create contact record
                var contact = new Contact
                {
                    Id = Guid.NewGuid().ToString(),
                    VoterId = request.VoterId,
                    VolunteerId = currentUserId,
                    Status = contactStatus,
                    VoterSupport = voterSupport,
                    Notes = request.Notes,
                    Timestamp = DateTime.UtcNow,
                    LocationLatitude = request.Location?.Latitude,
                    LocationLongitude = request.Location?.Longitude
                };

                _context.Contacts.Add(contact);

                // Update voter's contact status and support level
                voter.IsContacted = true;
                voter.LastContactStatus = contactStatus;
                if (voterSupport.HasValue)
                {
                    voter.VoterSupport = voterSupport.Value;
                }

                _logger.LogInformation("Saving contact to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Contact {ContactId} saved successfully to database", contact.Id);

                // Send notification emails to all super admins
                _logger.LogInformation("Starting notification process for contact {ContactId}", contact.Id);
                await SendContactNotificationToSuperAdmins(contact, voter, currentUserId);
                _logger.LogInformation("Notification process completed for contact {ContactId}", contact.Id);

                var contactDto = new ContactDto
                {
                    Id = contact.Id,
                    VoterId = contact.VoterId,
                    VolunteerId = contact.VolunteerId,
                    Status = contact.Status.ToString().ToLower(),
                    VoterSupport = contact.VoterSupport?.ToString().ToLower(),
                    Notes = contact.Notes,
                    Timestamp = contact.Timestamp,
                    Location = contact.LocationLatitude.HasValue && contact.LocationLongitude.HasValue
                        ? new LocationDto
                        {
                            Latitude = contact.LocationLatitude.Value,
                            Longitude = contact.LocationLongitude.Value
                        }
                        : null
                };

                _logger.LogInformation("Contact created: {ContactId} for voter {VoterId} by volunteer {VolunteerId}",
                    contact.Id, contact.VoterId, contact.VolunteerId);
                _logger.LogInformation("=== CONTACT CREATION COMPLETED SUCCESSFULLY ===");

                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contactDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact for voter {VoterId}", request.VoterId);
                return StatusCode(500, new { error = "Failed to create contact" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDto>> GetContact(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var contact = await _context.Contacts
                    .Include(c => c.Voter)
                    .Include(c => c.Volunteer)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contact == null)
                {
                    return NotFound();
                }

                // Check access permissions
                if (currentUserRole != "Admin" && contact.VolunteerId != currentUserId)
                {
                    return Forbid();
                }

                var contactDto = new ContactDto
                {
                    Id = contact.Id,
                    VoterId = contact.VoterId,
                    VolunteerId = contact.VolunteerId,
                    Status = contact.Status.ToString().ToLower(),
                    Notes = contact.Notes,
                    Timestamp = contact.Timestamp,
                    Location = contact.LocationLatitude.HasValue && contact.LocationLongitude.HasValue
                        ? new LocationDto
                        {
                            Latitude = contact.LocationLatitude.Value,
                            Longitude = contact.LocationLongitude.Value
                        }
                        : null
                };

                return Ok(contactDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact {ContactId}", id);
                return StatusCode(500, new { error = "Failed to retrieve contact" });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ContactListResponse>> GetContacts(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 25,
            [FromQuery] string? volunteerId = null,
            [FromQuery] string? voterId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var query = _context.Contacts.Include(c => c.Voter).Include(c => c.Volunteer).AsQueryable();

                // Apply access control
                if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
                {
                    query = query.Where(c => c.VolunteerId == currentUserId);
                }
                else if (!string.IsNullOrEmpty(volunteerId))
                {
                    query = query.Where(c => c.VolunteerId == volunteerId);
                }

                // Apply filters
                if (!string.IsNullOrEmpty(voterId))
                {
                    query = query.Where(c => c.VoterId == voterId);
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ContactStatus>(status, true, out var contactStatus))
                {
                    query = query.Where(c => c.Status == contactStatus);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(c => c.Timestamp >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(c => c.Timestamp <= toDate.Value);
                }

                // Order by timestamp descending
                query = query.OrderByDescending(c => c.Timestamp);

                var total = await query.CountAsync();
                var contacts = await query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var response = new ContactListResponse
                {
                    Contacts = contacts.Select(c => new ContactDto
                    {
                        Id = c.Id,
                        VoterId = c.VoterId,
                        VolunteerId = c.VolunteerId,
                        Status = c.Status.ToString().ToLower(),
                        VoterSupport = c.VoterSupport?.ToString().ToLower(),
                        Notes = c.Notes,
                        Timestamp = c.Timestamp,
                        VoterName = $"{c.Voter.FirstName} {c.Voter.LastName}",
                        VolunteerName = $"{c.Volunteer.FirstName} {c.Volunteer.LastName}",
                        Location = c.LocationLatitude.HasValue && c.LocationLongitude.HasValue
                            ? new LocationDto
                            {
                                Latitude = c.LocationLatitude.Value,
                                Longitude = c.LocationLongitude.Value
                            }
                            : null
                    }).ToList(),
                    Total = total,
                    Page = page,
                    TotalPages = (int)Math.Ceiling((double)total / limit)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts");
                return StatusCode(500, new { error = "Failed to retrieve contacts" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ContactDto>> UpdateContact(string id, [FromBody] UpdateContactRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var contact = await _context.Contacts
                    .Include(c => c.Voter)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contact == null)
                {
                    return NotFound();
                }

                // Check permissions
                if (currentUserRole != "Admin" && contact.VolunteerId != currentUserId)
                {
                    return Forbid();
                }

                // Update contact
                if (!string.IsNullOrEmpty(request.Status) && 
                    Enum.TryParse<ContactStatus>(request.Status, true, out var newStatus))
                {
                    contact.Status = newStatus;
                    contact.Voter.LastContactStatus = newStatus;
                }

                if (request.Notes != null)
                {
                    contact.Notes = request.Notes;
                }

                await _context.SaveChangesAsync();

                var contactDto = new ContactDto
                {
                    Id = contact.Id,
                    VoterId = contact.VoterId,
                    VolunteerId = contact.VolunteerId,
                    Status = contact.Status.ToString().ToLower(),
                    Notes = contact.Notes,
                    Timestamp = contact.Timestamp,
                    Location = contact.LocationLatitude.HasValue && contact.LocationLongitude.HasValue
                        ? new LocationDto
                        {
                            Latitude = contact.LocationLatitude.Value,
                            Longitude = contact.LocationLongitude.Value
                        }
                        : null
                };

                return Ok(contactDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {ContactId}", id);
                return StatusCode(500, new { error = "Failed to update contact" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> DeleteContact(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                
                _logger.LogInformation("=== CONTACT DELETION STARTED ===");
                _logger.LogInformation("Contact deletion request received from SuperAdmin {UserId} ({UserEmail}) for contact {ContactId}", 
                    currentUserId, currentUserEmail, id);

                var contact = await _context.Contacts
                    .Include(c => c.Voter)
                    .Include(c => c.Volunteer)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contact == null)
                {
                    _logger.LogWarning("Contact deletion failed: Contact {ContactId} not found", id);
                    return NotFound(new { error = "Contact not found" });
                }

                _logger.LogInformation("Contact found: {ContactId} for voter {VoterName} by volunteer {VolunteerName}", 
                    contact.Id, $"{contact.Voter.FirstName} {contact.Voter.LastName}", 
                    $"{contact.Volunteer.FirstName} {contact.Volunteer.LastName}");

                // Store voter reference before deletion
                var voter = contact.Voter;
                var voterId = contact.VoterId;

                // Remove the contact
                _context.Contacts.Remove(contact);
                
                // Check if this voter has any other contacts remaining
                var remainingContacts = await _context.Contacts
                    .Where(c => c.VoterId == voterId && c.Id != id)
                    .OrderByDescending(c => c.Timestamp)
                    .ToListAsync();

                if (!remainingContacts.Any())
                {
                    // No other contacts exist - reset voter to uncontacted state
                    _logger.LogInformation("No remaining contacts for voter {VoterId} - resetting to uncontacted state", voterId);
                    voter.IsContacted = false;
                    voter.LastContactStatus = null;
                    voter.VoterSupport = null;
                }
                else
                {
                    // Update voter status to reflect the most recent remaining contact
                    var latestContact = remainingContacts.First();
                    _logger.LogInformation("Updating voter {VoterId} status to reflect latest remaining contact {LatestContactId}", 
                        voterId, latestContact.Id);
                    voter.LastContactStatus = latestContact.Status;
                    
                    // Update voter support if the latest contact has support info
                    if (latestContact.VoterSupport.HasValue)
                    {
                        voter.VoterSupport = latestContact.VoterSupport.Value;
                    }
                    else
                    {
                        // Find the most recent contact with voter support
                        var contactWithSupport = remainingContacts
                            .FirstOrDefault(c => c.VoterSupport.HasValue);
                        voter.VoterSupport = contactWithSupport?.VoterSupport;
                    }
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Contact {ContactId} deleted successfully by SuperAdmin {UserEmail}", id, currentUserEmail);
                _logger.LogInformation("Voter {VoterId} status updated - IsContacted: {IsContacted}, LastStatus: {LastStatus}", 
                    voterId, voter.IsContacted, voter.LastContactStatus);

                // Send notification to all super admins about the deletion
                await SendContactDeletionNotificationToSuperAdmins(contact, voter, currentUserId);

                _logger.LogInformation("=== CONTACT DELETION COMPLETED SUCCESSFULLY ===");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {ContactId}", id);
                return StatusCode(500, new { error = "Failed to delete contact" });
            }
        }

        private async Task SendContactNotificationToSuperAdmins(Contact contact, Voter voter, string volunteerId)
        {
            try
            {
                _logger.LogInformation("=== NOTIFICATION PROCESS STARTED ===");
                _logger.LogInformation("Attempting to send notifications for contact {ContactId}", contact.Id);
                
                // Get all super admins
                _logger.LogInformation("Fetching super admins from database...");
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                _logger.LogInformation("Found {SuperAdminCount} super admins in system", superAdmins.Count);
                
                if (!superAdmins.Any())
                {
                    _logger.LogWarning("No super admins found to notify about contact {ContactId}", contact.Id);
                    return;
                }
                
                // Log details of each super admin
                foreach (var admin in superAdmins)
                {
                    _logger.LogInformation("Super admin found: {AdminEmail} (Active: {IsActive})", admin.Email, admin.IsActive);
                }

                // Get volunteer details
                _logger.LogInformation("Fetching volunteer details for ID: {VolunteerId}", volunteerId);
                var volunteer = await _userManager.FindByIdAsync(volunteerId);
                if (volunteer == null)
                {
                    _logger.LogError("Volunteer {VolunteerId} not found for contact notification", volunteerId);
                    return;
                }
                _logger.LogInformation("Volunteer found: {VolunteerName} ({VolunteerEmail})", 
                    $"{volunteer.FirstName} {volunteer.LastName}", volunteer.Email);

                // Prepare notification data
                var voterAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}";
                var location = contact.LocationLatitude.HasValue && contact.LocationLongitude.HasValue 
                    ? $"{contact.LocationLatitude:F6}, {contact.LocationLongitude:F6}"
                    : null;

                var notificationData = new ContactNotificationData
                {
                    VolunteerName = $"{volunteer.FirstName} {volunteer.LastName}",
                    VolunteerEmail = volunteer.Email!,
                    VoterName = $"{voter.FirstName} {voter.LastName}",
                    VoterAddress = voterAddress,
                    ContactStatus = contact.Status.ToString(),
                    VoterSupport = contact.VoterSupport?.ToString(),
                    Notes = contact.Notes,
                    ContactTime = contact.Timestamp,
                    Location = location
                };

                _logger.LogInformation("Prepared notification data - Volunteer: {VolunteerName}, Voter: {VoterName}, Status: {Status}", 
                    notificationData.VolunteerName, notificationData.VoterName, notificationData.ContactStatus);

                // Send notifications to all super admins
                _logger.LogInformation("Sending {EmailCount} notification emails...", superAdmins.Count);
                var notificationTasks = superAdmins.Select(admin => {
                    _logger.LogInformation("Queuing email to super admin: {AdminEmail}", admin.Email);
                    return _emailService.SendContactNotificationEmailAsync(admin.Email!, notificationData);
                }).ToArray();

                var results = await Task.WhenAll(notificationTasks);
                var successCount = results.Count(r => r);
                var failureCount = results.Length - successCount;

                _logger.LogInformation("Contact notification sent: {SuccessCount} succeeded, {FailureCount} failed for contact {ContactId}", 
                    successCount, failureCount, contact.Id);

                if (failureCount > 0)
                {
                    _logger.LogWarning("Some contact notification emails failed for contact {ContactId}", contact.Id);
                }
                
                _logger.LogInformation("=== NOTIFICATION PROCESS COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact notifications for contact {ContactId}", contact.Id);
                // Don't throw - we don't want notification failures to break contact creation
            }
        }

        private async Task SendContactDeletionNotificationToSuperAdmins(Contact contact, Voter voter, string superAdminId)
        {
            try
            {
                _logger.LogInformation("=== CONTACT DELETION NOTIFICATION PROCESS STARTED ===");
                _logger.LogInformation("Attempting to send deletion notifications for contact {ContactId}", contact.Id);
                
                // Get all super admins
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                _logger.LogInformation("Found {SuperAdminCount} super admins for deletion notification", superAdmins.Count);
                
                if (!superAdmins.Any())
                {
                    _logger.LogWarning("No super admins found to notify about contact deletion {ContactId}", contact.Id);
                    return;
                }

                // Get the super admin who performed the deletion
                var deletingAdmin = await _userManager.FindByIdAsync(superAdminId);
                if (deletingAdmin == null)
                {
                    _logger.LogError("Deleting SuperAdmin {SuperAdminId} not found for deletion notification", superAdminId);
                    return;
                }

                // Get volunteer details
                var volunteer = contact.Volunteer;
                
                // Prepare deletion notification data
                var voterAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}";
                var location = contact.LocationLatitude.HasValue && contact.LocationLongitude.HasValue 
                    ? $"{contact.LocationLatitude:F6}, {contact.LocationLongitude:F6}"
                    : null;

                var deletionData = new ContactDeletionNotificationData
                {
                    DeletedByName = $"{deletingAdmin.FirstName} {deletingAdmin.LastName}",
                    DeletedByEmail = deletingAdmin.Email!,
                    VolunteerName = $"{volunteer.FirstName} {volunteer.LastName}",
                    VolunteerEmail = volunteer.Email!,
                    VoterName = $"{voter.FirstName} {voter.LastName}",
                    VoterAddress = voterAddress,
                    ContactStatus = contact.Status.ToString(),
                    VoterSupport = contact.VoterSupport?.ToString(),
                    Notes = contact.Notes,
                    OriginalContactTime = contact.Timestamp,
                    DeletionTime = DateTime.UtcNow,
                    Location = location,
                    VoterNewStatus = voter.IsContacted ? "Still Contacted" : "Reset to Uncontacted"
                };

                _logger.LogInformation("Prepared deletion notification data - Deleted by: {DeletedBy}, Contact: {ContactId}", 
                    deletionData.DeletedByName, contact.Id);

                // Send notifications to all super admins
                var notificationTasks = superAdmins.Select(admin => {
                    _logger.LogInformation("Queuing deletion notification email to super admin: {AdminEmail}", admin.Email);
                    return _emailService.SendContactDeletionNotificationEmailAsync(admin.Email!, deletionData);
                }).ToArray();

                var results = await Task.WhenAll(notificationTasks);
                var successCount = results.Count(r => r);
                var failureCount = results.Length - successCount;

                _logger.LogInformation("Contact deletion notification sent: {SuccessCount} succeeded, {FailureCount} failed for contact {ContactId}", 
                    successCount, failureCount, contact.Id);

                _logger.LogInformation("=== CONTACT DELETION NOTIFICATION PROCESS COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact deletion notifications for contact {ContactId}", contact.Id);
                // Don't throw - we don't want notification failures to break contact deletion
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // km
            const double metersPerKm = 1000;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distanceKm = earthRadius * c;
            
            return distanceKm * metersPerKm; // Return distance in meters
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    public class CreateContactRequest
    {
        public string VoterId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public LocationDto? Location { get; set; }
    }

    public class UpdateContactRequest
    {
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class ContactDto
    {
        public string Id { get; set; } = string.Empty;
        public string VoterId { get; set; } = string.Empty;
        public string VolunteerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
        public string? VoterName { get; set; }
        public string? VolunteerName { get; set; }
        public LocationDto? Location { get; set; }
    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ContactListResponse
    {
        public List<ContactDto> Contacts { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

}