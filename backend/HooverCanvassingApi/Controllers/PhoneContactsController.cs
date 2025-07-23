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
    public class PhoneContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PhoneContactsController> _logger;
        private readonly IEmailService _emailService;
        private readonly UserManager<Volunteer> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorageService;

        public PhoneContactsController(
            ApplicationDbContext context, 
            ILogger<PhoneContactsController> logger, 
            IEmailService emailService, 
            UserManager<Volunteer> userManager, 
            IConfiguration configuration,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
            _fileStorageService = fileStorageService;
        }

        [HttpPost]
        public async Task<ActionResult<PhoneContact>> CreatePhoneContact([FromBody] CreatePhoneContactDto dto)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("User ID not found in claims");
            }

            var voter = await _context.Voters.FindAsync(dto.VoterId);
            if (voter == null)
            {
                return NotFound("Voter not found");
            }

            var phoneContact = new PhoneContact
            {
                VoterId = dto.VoterId,
                VolunteerId = currentUserId,
                Status = dto.Status,
                VoterSupport = dto.VoterSupport,
                Notes = dto.Notes,
                AudioFileUrl = dto.AudioFileUrl,
                AudioDurationSeconds = dto.AudioDurationSeconds,
                CallDurationSeconds = dto.CallDurationSeconds,
                PhoneNumberUsed = dto.PhoneNumberUsed
            };

            _context.PhoneContacts.Add(phoneContact);

            // Update voter status
            voter.IsContacted = true;
            voter.LastContactStatus = ConvertPhoneStatusToContactStatus(dto.Status);
            if (dto.VoterSupport.HasValue)
            {
                voter.VoterSupport = dto.VoterSupport.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Phone contact created: {ContactId} for voter {VoterId} by volunteer {VolunteerId} with status {Status}",
                phoneContact.Id, phoneContact.VoterId, phoneContact.VolunteerId, phoneContact.Status);

            return Ok(phoneContact);
        }

        [HttpGet("voter/{voterId}")]
        public async Task<ActionResult<IEnumerable<PhoneContactDto>>> GetPhoneContactsForVoter(string voterId)
        {
            var contacts = await _context.PhoneContacts
                .Include(pc => pc.Volunteer)
                .Where(pc => pc.VoterId == voterId)
                .OrderByDescending(pc => pc.Timestamp)
                .Select(pc => new PhoneContactDto
                {
                    Id = pc.Id,
                    VoterId = pc.VoterId,
                    VolunteerId = pc.VolunteerId,
                    VolunteerName = $"{pc.Volunteer.FirstName} {pc.Volunteer.LastName}",
                    Status = pc.Status,
                    VoterSupport = pc.VoterSupport,
                    Notes = pc.Notes,
                    Timestamp = pc.Timestamp,
                    CallDurationSeconds = pc.CallDurationSeconds,
                    PhoneNumberUsed = pc.PhoneNumberUsed,
                    AudioFileUrl = pc.AudioFileUrl,
                    AudioDurationSeconds = pc.AudioDurationSeconds
                })
                .ToListAsync();

            return Ok(contacts);
        }

        [HttpGet("my-contacts")]
        public async Task<ActionResult<PhoneContactsSummaryDto>> GetMyPhoneContacts([FromQuery] DateTime? date)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("User ID not found in claims");
            }

            var query = _context.PhoneContacts
                .Include(pc => pc.Voter)
                .Where(pc => pc.VolunteerId == currentUserId);

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(pc => pc.Timestamp >= startDate && pc.Timestamp < endDate);
            }

            var contacts = await query
                .OrderByDescending(pc => pc.Timestamp)
                .Select(pc => new PhoneContactDetailDto
                {
                    Id = pc.Id,
                    VoterId = pc.VoterId,
                    VoterName = $"{pc.Voter.FirstName} {pc.Voter.LastName}",
                    VoterAddress = $"{pc.Voter.AddressLine}, {pc.Voter.City}, {pc.Voter.State} {pc.Voter.Zip}",
                    VoterPhone = pc.Voter.CellPhone,
                    Status = pc.Status,
                    VoterSupport = pc.VoterSupport,
                    Notes = pc.Notes,
                    Timestamp = pc.Timestamp,
                    CallDurationSeconds = pc.CallDurationSeconds,
                    PhoneNumberUsed = pc.PhoneNumberUsed
                })
                .ToListAsync();

            var summary = new PhoneContactsSummaryDto
            {
                TotalContacts = contacts.Count,
                ContactsByStatus = contacts.GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ContactsBySupport = contacts
                    .Where(c => c.VoterSupport.HasValue)
                    .GroupBy(c => c.VoterSupport!.Value)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TotalCallDuration = contacts.Sum(c => c.CallDurationSeconds ?? 0),
                Contacts = contacts
            };

            return Ok(summary);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<PhoneContactStatsDto>> GetPhoneContactStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.PhoneContacts.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(pc => pc.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(pc => pc.Timestamp < endDate.Value.AddDays(1));
            }

            var stats = await query
                .GroupBy(pc => new { pc.VolunteerId, pc.Status })
                .Select(g => new
                {
                    VolunteerId = g.Key.VolunteerId,
                    Status = g.Key.Status,
                    Count = g.Count()
                })
                .ToListAsync();

            var volunteerIds = stats.Select(s => s.VolunteerId).Distinct().ToList();
            var volunteers = await _context.Volunteers
                .Where(v => volunteerIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => $"{v.FirstName} {v.LastName}");

            var result = new PhoneContactStatsDto
            {
                TotalCalls = stats.Sum(s => s.Count),
                CallsByStatus = stats
                    .GroupBy(s => s.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Sum(s => s.Count)),
                TopVolunteers = stats
                    .GroupBy(s => s.VolunteerId)
                    .Select(g => new VolunteerCallStatsDto
                    {
                        VolunteerId = g.Key,
                        VolunteerName = volunteers.GetValueOrDefault(g.Key, "Unknown"),
                        TotalCalls = g.Sum(s => s.Count),
                        CallsByStatus = g.ToDictionary(s => s.Status.ToString(), s => s.Count)
                    })
                    .OrderByDescending(v => v.TotalCalls)
                    .Take(10)
                    .ToList()
            };

            return Ok(result);
        }

        [HttpPost("upload-audio")]
        public async Task<ActionResult<UploadAudioResponse>> UploadAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No audio file provided");
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("User ID not found in claims");
            }

            try
            {
                var fileExtension = Path.GetExtension(audioFile.FileName);
                var fileName = $"phone-contact-{currentUserId}-{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                
                string audioUrl;
                using (var stream = audioFile.OpenReadStream())
                {
                    audioUrl = await _fileStorageService.UploadAudioAsync(stream, fileName);
                }

                return Ok(new UploadAudioResponse
                {
                    AudioUrl = audioUrl,
                    FileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload audio file");
                return StatusCode(500, "Failed to upload audio file");
            }
        }

        private ContactStatus ConvertPhoneStatusToContactStatus(PhoneContactStatus phoneStatus)
        {
            return phoneStatus switch
            {
                PhoneContactStatus.Reached => ContactStatus.Reached,
                PhoneContactStatus.NoAnswer => ContactStatus.NotHome,
                PhoneContactStatus.VoiceMail => ContactStatus.NotHome,
                PhoneContactStatus.Refused => ContactStatus.Refused,
                PhoneContactStatus.Callback => ContactStatus.NeedsFollowUp,
                _ => ContactStatus.NotHome
            };
        }
    }

    public class CreatePhoneContactDto
    {
        public string VoterId { get; set; } = string.Empty;
        public PhoneContactStatus Status { get; set; }
        public VoterSupport? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public string? AudioFileUrl { get; set; }
        public int? AudioDurationSeconds { get; set; }
        public int? CallDurationSeconds { get; set; }
        public string? PhoneNumberUsed { get; set; }
    }

    public class PhoneContactDto
    {
        public string Id { get; set; } = string.Empty;
        public string VoterId { get; set; } = string.Empty;
        public string VolunteerId { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public PhoneContactStatus Status { get; set; }
        public VoterSupport? VoterSupport { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
        public int? CallDurationSeconds { get; set; }
        public string? PhoneNumberUsed { get; set; }
        public string? AudioFileUrl { get; set; }
        public int? AudioDurationSeconds { get; set; }
    }

    public class PhoneContactDetailDto : PhoneContactDto
    {
        public string VoterName { get; set; } = string.Empty;
        public string VoterAddress { get; set; } = string.Empty;
        public string? VoterPhone { get; set; }
    }

    public class PhoneContactsSummaryDto
    {
        public int TotalContacts { get; set; }
        public Dictionary<string, int> ContactsByStatus { get; set; } = new();
        public Dictionary<string, int> ContactsBySupport { get; set; } = new();
        public int TotalCallDuration { get; set; }
        public List<PhoneContactDetailDto> Contacts { get; set; } = new();
    }

    public class PhoneContactStatsDto
    {
        public int TotalCalls { get; set; }
        public Dictionary<string, int> CallsByStatus { get; set; } = new();
        public List<VolunteerCallStatsDto> TopVolunteers { get; set; } = new();
    }

    public class VolunteerCallStatsDto
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public Dictionary<string, int> CallsByStatus { get; set; } = new();
    }

    public class UploadAudioResponse
    {
        public string AudioUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}