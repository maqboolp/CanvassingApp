using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;
using HooverCanvassingApi;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly VoterImportService _importService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, VoterImportService importService, ILogger<AdminController> logger)
        {
            _context = context;
            _importService = importService;
            _logger = logger;
        }

        [HttpPost("import-voters")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<ImportResult>> ImportVoters(IFormFile file, [FromQuery] bool enableGeocoding = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "File must be a CSV" });
                }

                using var stream = file.OpenReadStream();
                var result = await _importService.ImportVotersFromCsvAsync(stream, enableGeocoding);

                _logger.LogInformation("Voter import completed by admin. Imported: {ImportedCount}, Errors: {ErrorCount}",
                    result.ImportedCount, result.ErrorCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during voter import");
                return StatusCode(500, new { error = "Import failed", details = ex.Message });
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<AnalyticsDto>> GetAnalytics()
        {
            try
            {
                var totalVoters = await _context.Voters.CountAsync();
                var totalContacted = await _context.Voters.CountAsync(v => v.IsContacted);

                var contactStatusBreakdown = await _context.Contacts
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var volunteerActivity = await _context.Volunteers
                    .Where(v => v.Role == VolunteerRole.Volunteer && v.IsActive)
                    .Select(v => new VolunteerActivityDto
                    {
                        VolunteerId = v.Id,
                        VolunteerName = $"{v.FirstName} {v.LastName}",
                        ContactsToday = v.Contacts.Count(c => c.Timestamp.Date == DateTime.UtcNow.Date),
                        ContactsTotal = v.Contacts.Count()
                    })
                    .ToListAsync();

                var contactsByZip = await _context.Voters
                    .GroupBy(v => v.Zip)
                    .Select(g => new ContactsByZipDto
                    {
                        ZipCode = g.Key,
                        Contacted = g.Count(v => v.IsContacted),
                        Total = g.Count()
                    })
                    .OrderBy(x => x.ZipCode)
                    .ToListAsync();

                var analytics = new AnalyticsDto
                {
                    TotalVoters = totalVoters,
                    TotalContacted = totalContacted,
                    ContactStatusBreakdown = new ContactStatusBreakdownDto
                    {
                        Reached = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Reached) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Reached).Count : 0,
                        NotHome = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NotHome) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NotHome).Count : 0,
                        Refused = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Refused) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Refused).Count : 0,
                        NeedsFollowUp = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NeedsFollowUp) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NeedsFollowUp).Count : 0
                    },
                    VolunteerActivity = volunteerActivity,
                    ContactsByZip = contactsByZip
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new { error = "Failed to retrieve analytics" });
            }
        }

        [HttpGet("volunteers")]
        public async Task<ActionResult<List<VolunteerDto>>> GetVolunteers()
        {
            try
            {
                var volunteers = await _context.Volunteers
                    .Select(v => new VolunteerDto
                    {
                        Id = v.Id,
                        Email = v.Email!,
                        FirstName = v.FirstName,
                        LastName = v.LastName,
                        PhoneNumber = v.PhoneNumber,
                        Role = v.Role.ToString(),
                        IsActive = v.IsActive,
                        CreatedAt = v.CreatedAt,
                        ContactCount = v.Contacts.Count()
                    })
                    .OrderBy(v => v.LastName)
                    .ToListAsync();

                return Ok(volunteers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving volunteers");
                return StatusCode(500, new { error = "Failed to retrieve volunteers" });
            }
        }

        [HttpPost("geocode-voters")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> GeocodeVoters()
        {
            try
            {
                var result = await _importService.GeocodeExistingVotersAsync();
                return Ok(new { 
                    message = "Geocoding completed successfully",
                    processed = result.ProcessedCount,
                    geocoded = result.GeocodedCount,
                    failed = result.FailedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding voters");
                return StatusCode(500, new { error = "Geocoding failed", details = ex.Message });
            }
        }

        [HttpGet("voter-contact-history")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> GetVoterContactHistory(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 25,
            [FromQuery] string? search = null,
            [FromQuery] string? volunteerId = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var query = _context.Contacts
                    .Include(c => c.Voter)
                    .Include(c => c.Volunteer)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Voter.FirstName.Contains(search) ||
                        c.Voter.LastName.Contains(search) ||
                        c.VoterId.Contains(search));
                }

                // Apply volunteer filter
                if (!string.IsNullOrEmpty(volunteerId))
                {
                    query = query.Where(c => c.VolunteerId == volunteerId);
                }

                // Apply date range filters
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
                {
                    query = query.Where(c => c.Timestamp.Date >= start.Date);
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
                {
                    query = query.Where(c => c.Timestamp.Date <= end.Date);
                }

                var total = await query.CountAsync();

                var contactsFromDb = await query
                    .OrderByDescending(c => c.Timestamp)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var contacts = contactsFromDb.Select(c => new
                {
                    Id = c.Id,
                    VoterId = c.VoterId,
                    VoterName = $"{c.Voter.FirstName} {c.Voter.LastName}",
                    VoterAddress = $"{c.Voter.AddressLine}, {c.Voter.City}, {c.Voter.State} {c.Voter.Zip}",
                    VolunteerId = c.VolunteerId,
                    VolunteerName = $"{c.Volunteer.FirstName} {c.Volunteer.LastName}",
                    ContactDate = c.Timestamp,
                    Status = c.Status.ToString().ToLower().Replace("home", "-home").Replace("followup", "follow-up"),
                    VoterSupport = c.VoterSupport?.ToString().ToLower(),
                    Notes = c.Notes
                }).ToList();

                var totalPages = (int)Math.Ceiling((double)total / limit);

                return Ok(new
                {
                    Contacts = contacts,
                    Total = total,
                    Page = page,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voter contact history");
                return StatusCode(500, new { error = "Failed to retrieve voter contact history" });
            }
        }

        [HttpGet("geocoding-status")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> GetGeocodingStatus()
        {
            try
            {
                var totalVoters = await _context.Voters.CountAsync();
                var geocodedVoters = await _context.Voters.CountAsync(v => v.Latitude.HasValue && v.Longitude.HasValue);
                var pendingVoters = totalVoters - geocodedVoters;

                return Ok(new
                {
                    totalVoters,
                    geocodedVoters,
                    pendingVoters,
                    geocodingPercentage = totalVoters > 0 ? (double)geocodedVoters / totalVoters * 100 : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting geocoding status");
                return StatusCode(500, new { error = "Failed to get geocoding status" });
            }
        }

        [HttpGet("export-analytics")]
        public async Task<ActionResult> ExportAnalytics()
        {
            try
            {
                var analytics = await GetAnalyticsData();
                
                var csv = GenerateAnalyticsCsv(analytics);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                
                return File(bytes, "text/csv", $"analytics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics");
                return StatusCode(500, new { error = "Failed to export analytics" });
            }
        }

        private async Task<AnalyticsDto> GetAnalyticsData()
        {
            // This is a simplified version of the analytics endpoint
            var totalVoters = await _context.Voters.CountAsync();
            var totalContacted = await _context.Voters.CountAsync(v => v.IsContacted);

            var contactStatusBreakdown = await _context.Contacts
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var volunteerActivity = await _context.Volunteers
                .Where(v => v.Role == VolunteerRole.Volunteer && v.IsActive)
                .Select(v => new VolunteerActivityDto
                {
                    VolunteerId = v.Id,
                    VolunteerName = $"{v.FirstName} {v.LastName}",
                    ContactsToday = v.Contacts.Count(c => c.Timestamp.Date == DateTime.UtcNow.Date),
                    ContactsTotal = v.Contacts.Count()
                })
                .ToListAsync();

            var contactsByZip = await _context.Voters
                .GroupBy(v => v.Zip)
                .Select(g => new ContactsByZipDto
                {
                    ZipCode = g.Key,
                    Contacted = g.Count(v => v.IsContacted),
                    Total = g.Count()
                })
                .ToListAsync();

            return new AnalyticsDto
            {
                TotalVoters = totalVoters,
                TotalContacted = totalContacted,
                ContactStatusBreakdown = new ContactStatusBreakdownDto
                {
                    Reached = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Reached) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Reached).Count : 0,
                    NotHome = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NotHome) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NotHome).Count : 0,
                    Refused = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Refused) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.Refused).Count : 0,
                    NeedsFollowUp = contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NeedsFollowUp) != null ? contactStatusBreakdown.FirstOrDefault(x => x.Status == ContactStatus.NeedsFollowUp).Count : 0
                },
                VolunteerActivity = volunteerActivity,
                ContactsByZip = contactsByZip
            };
        }

        private static string GenerateAnalyticsCsv(AnalyticsDto analytics)
        {
            var csv = new System.Text.StringBuilder();
            
            csv.AppendLine("Hoover Canvassing Analytics Report");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine();
            
            csv.AppendLine("Summary");
            csv.AppendLine($"Total Voters,{analytics.TotalVoters}");
            csv.AppendLine($"Total Contacted,{analytics.TotalContacted}");
            csv.AppendLine($"Contact Rate,{(analytics.TotalVoters > 0 ? (double)analytics.TotalContacted / analytics.TotalVoters * 100 : 0):F1}%");
            csv.AppendLine();
            
            csv.AppendLine("Contact Status Breakdown");
            csv.AppendLine($"Reached,{analytics.ContactStatusBreakdown.Reached}");
            csv.AppendLine($"Not Home,{analytics.ContactStatusBreakdown.NotHome}");
            csv.AppendLine($"Refused,{analytics.ContactStatusBreakdown.Refused}");
            csv.AppendLine($"Needs Follow-up,{analytics.ContactStatusBreakdown.NeedsFollowUp}");
            csv.AppendLine();
            
            csv.AppendLine("Volunteer Activity");
            csv.AppendLine("Volunteer Name,Total Contacts,Contacts Today");
            foreach (var volunteer in analytics.VolunteerActivity)
            {
                csv.AppendLine($"{volunteer.VolunteerName},{volunteer.ContactsTotal},{volunteer.ContactsToday}");
            }
            csv.AppendLine();
            
            csv.AppendLine("Contacts by ZIP Code");
            csv.AppendLine("ZIP Code,Contacted,Total,Contact Rate");
            foreach (var zip in analytics.ContactsByZip)
            {
                var rate = zip.Total > 0 ? (double)zip.Contacted / zip.Total * 100 : 0;
                csv.AppendLine($"{zip.ZipCode},{zip.Contacted},{zip.Total},{rate:F1}%");
            }
            
            return csv.ToString();
        }

        [HttpPost("initialize-database")]
        [AllowAnonymous] // Temporary - remove after initial setup
        public async Task<IActionResult> InitializeDatabase()
        {
            try
            {
                // Run migrations
                await _context.Database.MigrateAsync();
                
                // Initialize seed data
                await SeedData.InitializeAsync(HttpContext.RequestServices);
                
                return Ok(new { 
                    success = true, 
                    message = "Database initialized successfully",
                    data = new {
                        migrationsApplied = true,
                        seedDataCreated = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Failed to initialize database",
                    message = ex.Message,
                    details = ex.ToString()
                });
            }
        }
    }

    public class VolunteerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ContactCount { get; set; }
    }

    public class AnalyticsDto
    {
        public int TotalVoters { get; set; }
        public int TotalContacted { get; set; }
        public ContactStatusBreakdownDto ContactStatusBreakdown { get; set; } = new();
        public List<VolunteerActivityDto> VolunteerActivity { get; set; } = new();
        public List<ContactsByZipDto> ContactsByZip { get; set; } = new();
    }

    public class ContactStatusBreakdownDto
    {
        public int Reached { get; set; }
        public int NotHome { get; set; }
        public int Refused { get; set; }
        public int NeedsFollowUp { get; set; }
    }

    public class VolunteerActivityDto
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public int ContactsToday { get; set; }
        public int ContactsTotal { get; set; }
    }

    public class ContactsByZipDto
    {
        public string ZipCode { get; set; } = string.Empty;
        public int Contacted { get; set; }
        public int Total { get; set; }
    }
}