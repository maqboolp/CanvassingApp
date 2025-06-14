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

        public ContactsController(ApplicationDbContext context, ILogger<ContactsController> logger, IEmailService emailService, UserManager<Volunteer> userManager)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Validate voter exists
                var voter = await _context.Voters
                    .FirstOrDefaultAsync(v => v.LalVoterId == request.VoterId);

                if (voter == null)
                {
                    return NotFound(new { error = "Voter not found" });
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

                await _context.SaveChangesAsync();

                // Send notification emails to all super admins
                await SendContactNotificationToSuperAdmins(contact, voter, currentUserId);

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
                if (currentUserRole != "Admin")
                {
                    query = query.Where(c => c.VolunteerId == currentUserId);
                }
                else if (!string.IsNullOrEmpty(volunteerId))
                {
                    query = query.Where(c => c.VolunteerId == volunteerId);
                }

                // Apply filters
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

        private async Task SendContactNotificationToSuperAdmins(Contact contact, Voter voter, string volunteerId)
        {
            try
            {
                // Get all super admins
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                
                if (!superAdmins.Any())
                {
                    _logger.LogWarning("No super admins found to notify about contact {ContactId}", contact.Id);
                    return;
                }

                // Get volunteer details
                var volunteer = await _userManager.FindByIdAsync(volunteerId);
                if (volunteer == null)
                {
                    _logger.LogError("Volunteer {VolunteerId} not found for contact notification", volunteerId);
                    return;
                }

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

                // Send notifications to all super admins
                var notificationTasks = superAdmins.Select(admin => 
                    _emailService.SendContactNotificationEmailAsync(admin.Email!, notificationData)
                ).ToArray();

                var results = await Task.WhenAll(notificationTasks);
                var successCount = results.Count(r => r);
                var failureCount = results.Length - successCount;

                _logger.LogInformation("Contact notification sent: {SuccessCount} succeeded, {FailureCount} failed for contact {ContactId}", 
                    successCount, failureCount, contact.Id);

                if (failureCount > 0)
                {
                    _logger.LogWarning("Some contact notification emails failed for contact {ContactId}", contact.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact notifications for contact {ContactId}", contact.Id);
                // Don't throw - we don't want notification failures to break contact creation
            }
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