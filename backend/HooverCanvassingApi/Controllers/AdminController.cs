using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<Volunteer> _userManager;
        private readonly IEmailService _emailService;

        public AdminController(ApplicationDbContext context, VoterImportService importService, ILogger<AdminController> logger, UserManager<Volunteer> userManager, IEmailService emailService)
        {
            _context = context;
            _importService = importService;
            _logger = logger;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpPost("import-voters")]
        [Authorize(Roles = "Admin,SuperAdmin")]
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

                // Get all active users (volunteers, admins, superadmins) who have made contacts
                var volunteerActivity = await _context.Volunteers
                    .Where(v => v.IsActive)
                    .Include(v => v.Contacts)
                    .ToListAsync();

                var volunteerActivityDto = volunteerActivity
                    .Where(v => v.Contacts.Any()) // Only include users who have made contacts
                    .Select(v => new VolunteerActivityDto
                    {
                        VolunteerId = v.Id,
                        VolunteerName = $"{v.FirstName} {v.LastName}",
                        ContactsToday = v.Contacts.Count(c => c.Timestamp.Date == DateTime.UtcNow.Date),
                        ContactsTotal = v.Contacts.Count()
                    })
                    .OrderByDescending(v => v.ContactsTotal)
                    .ToList();

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

                // Calculate login metrics
                var allVolunteers = await _context.Volunteers.ToListAsync();
                var weekAgo = DateTime.UtcNow.AddDays(-7);
                var monthAgo = DateTime.UtcNow.AddDays(-30);
                
                var totalLogins = allVolunteers.Sum(v => v.LoginCount);
                var activeUsersWeek = allVolunteers.Count(v => v.LastLoginAt >= weekAgo);
                var activeUsersMonth = allVolunteers.Count(v => v.LastLoginAt >= monthAgo);

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
                    VolunteerActivity = volunteerActivityDto,
                    ContactsByZip = contactsByZip,
                    TotalLogins = totalLogins,
                    ActiveUsersWeek = activeUsersWeek,
                    ActiveUsersMonth = activeUsersMonth
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new { error = "Failed to retrieve analytics" });
            }
        }

        [HttpGet("leaderboard")]
        public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard()
        {
            try
            {
                // Get this week's date range
                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);

                // Get this month's date range
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Get all active volunteers and their contacts
                var volunteers = await _context.Volunteers
                    .Where(v => v.IsActive)
                    .Include(v => v.Contacts)
                    .ToListAsync();

                // Calculate weekly leaderboard in memory
                var weeklyLeaderboard = volunteers
                    .Select(v => new LeaderboardEntry
                    {
                        VolunteerId = v.Id,
                        VolunteerName = $"{v.FirstName} {v.LastName}",
                        ContactCount = v.Contacts.Count(c => c.Timestamp >= weekStart && c.Timestamp < weekEnd),
                        IsCurrentUser = false // Admin view, so no current user highlighting
                    })
                    .Where(v => v.ContactCount > 0) // Only show users with contacts
                    .OrderByDescending(v => v.ContactCount)
                    .ThenBy(v => v.VolunteerName)
                    .Take(10)
                    .ToList();

                // Calculate monthly leaderboard in memory
                var monthlyLeaderboard = volunteers
                    .Select(v => new LeaderboardEntry
                    {
                        VolunteerId = v.Id,
                        VolunteerName = $"{v.FirstName} {v.LastName}",
                        ContactCount = v.Contacts.Count(c => c.Timestamp >= monthStart && c.Timestamp < monthEnd),
                        IsCurrentUser = false // Admin view, so no current user highlighting
                    })
                    .Where(v => v.ContactCount > 0) // Only show users with contacts
                    .OrderByDescending(v => v.ContactCount)
                    .ThenBy(v => v.VolunteerName)
                    .Take(10)
                    .ToList();

                // Assign positions and badges
                AssignPositionsAndBadges(weeklyLeaderboard);
                AssignPositionsAndBadges(monthlyLeaderboard);

                var response = new LeaderboardResponse
                {
                    WeeklyLeaderboard = weeklyLeaderboard,
                    MonthlyLeaderboard = monthlyLeaderboard,
                    CurrentUserAchievements = new List<Achievement>() // No achievements for admin view
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin leaderboard");
                return StatusCode(500, new { error = "Failed to retrieve leaderboard" });
            }
        }

        private void AssignPositionsAndBadges(List<LeaderboardEntry> leaderboard)
        {
            for (int i = 0; i < leaderboard.Count; i++)
            {
                leaderboard[i].Position = i + 1;
                leaderboard[i].Badge = (i + 1) switch
                {
                    1 => "🥇",
                    2 => "🥈", 
                    3 => "🥉",
                    _ => ""
                };
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
                        ContactCount = v.Contacts.Count(),
                        LoginCount = v.LoginCount,
                        LastLoginAt = v.LastLoginAt,
                        LastActivity = v.LastActivity
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
        // Fixed all null propagating operators in LINQ expressions - v3
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
                    VoterSupport = c.VoterSupport != null ? c.VoterSupport.ToString().ToLower() : null,
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

        [HttpPost("fix-database-schema")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> FixDatabaseSchema()
        {
            try
            {
                _logger.LogInformation("Starting database schema fix for all missing columns...");
                
                // Check if columns exist first
                var checkSql = @"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'AspNetUsers' 
                    AND column_name IN ('LoginCount', 'LastLoginAt', 'LastActivity');";
                
                var existingColumns = new List<string>();
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = checkSql;
                    await _context.Database.OpenConnectionAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            existingColumns.Add(reader.GetString(0));
                        }
                    }
                    await _context.Database.CloseConnectionAsync();
                }
                
                _logger.LogInformation("Existing columns: {ExistingColumns}", string.Join(", ", existingColumns));
                
                var sqlCommands = new List<string>();
                
                if (!existingColumns.Contains("LoginCount"))
                {
                    sqlCommands.Add("ALTER TABLE \"AspNetUsers\" ADD COLUMN \"LoginCount\" integer NOT NULL DEFAULT 0;");
                }
                
                if (!existingColumns.Contains("LastLoginAt"))
                {
                    sqlCommands.Add("ALTER TABLE \"AspNetUsers\" ADD COLUMN \"LastLoginAt\" timestamp with time zone NULL;");
                }
                
                if (!existingColumns.Contains("LastActivity"))
                {
                    sqlCommands.Add("ALTER TABLE \"AspNetUsers\" ADD COLUMN \"LastActivity\" timestamp with time zone NULL;");
                }
                
                if (sqlCommands.Any())
                {
                    _logger.LogInformation("Executing SQL commands: {Commands}", string.Join("; ", sqlCommands));
                    
                    foreach (var sql in sqlCommands)
                    {
                        await _context.Database.ExecuteSqlRawAsync(sql);
                        _logger.LogInformation("Executed: {SQL}", sql);
                    }
                }
                else
                {
                    _logger.LogInformation("All required columns already exist");
                }
                
                // Verify columns were added
                var verifyColumns = new List<string>();
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = checkSql;
                    await _context.Database.OpenConnectionAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            verifyColumns.Add(reader.GetString(0));
                        }
                    }
                    await _context.Database.CloseConnectionAsync();
                }
                
                var hasLoginCount = verifyColumns.Contains("LoginCount");
                var hasLastLoginAt = verifyColumns.Contains("LastLoginAt");
                var hasLastActivity = verifyColumns.Contains("LastActivity");
                
                _logger.LogInformation("After fix - LoginCount exists: {HasLoginCount}, LastLoginAt exists: {HasLastLoginAt}, LastActivity exists: {HasLastActivity}", 
                    hasLoginCount, hasLastLoginAt, hasLastActivity);
                
                return Ok(new { 
                    success = true, 
                    message = "Database schema fix completed",
                    data = new {
                        commandsExecuted = sqlCommands.Count,
                        hasLoginCount = hasLoginCount,
                        hasLastLoginAt = hasLastLoginAt,
                        hasLastActivity = hasLastActivity,
                        existingColumnsBefore = existingColumns,
                        existingColumnsAfter = verifyColumns
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing database schema");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Failed to fix database schema",
                    message = ex.Message,
                    details = ex.ToString()
                });
            }
        }

        [HttpPost("add-missing-column")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddMissingColumn()
        {
            try
            {
                _logger.LogInformation("Adding missing LastActivity column...");
                
                // Directly add the LastActivity column
                var sql = "ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"LastActivity\" timestamp with time zone NULL;";
                
                await _context.Database.ExecuteSqlRawAsync(sql);
                _logger.LogInformation("Successfully added LastActivity column");
                
                return Ok(new { 
                    success = true, 
                    message = "LastActivity column added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding LastActivity column");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Failed to add LastActivity column",
                    message = ex.Message
                });
            }
        }

        [HttpPost("initialize-database")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> InitializeDatabase()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");
                
                // Get pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                _logger.LogInformation("Pending migrations: {PendingMigrations}", string.Join(", ", pendingMigrations));
                
                // Run migrations
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");
                
                // Get applied migrations
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
                _logger.LogInformation("Applied migrations: {AppliedMigrations}", string.Join(", ", appliedMigrations));
                
                // Initialize seed data
                await SeedData.InitializeAsync(HttpContext.RequestServices);
                _logger.LogInformation("Seed data initialized successfully");
                
                return Ok(new { 
                    success = true, 
                    message = "Database initialized successfully",
                    data = new {
                        migrationsApplied = true,
                        seedDataCreated = true,
                        pendingMigrations = pendingMigrations.ToList(),
                        appliedMigrations = appliedMigrations.ToList()
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

        [HttpPost("change-user-role")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> ChangeUserRole([FromBody] ChangeRoleRequest request)
        {
            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.UserId) || !Enum.IsDefined(typeof(VolunteerRole), request.NewRole))
                {
                    return BadRequest(new { error = "Invalid user ID or role" });
                }

                // Find the user
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Prevent superadmin from demoting themselves
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (user.Id == currentUserId && request.NewRole != VolunteerRole.SuperAdmin)
                {
                    return BadRequest(new { error = "Cannot change your own role from SuperAdmin" });
                }

                // Get the user as Volunteer to update role
                var volunteer = await _context.Volunteers.FindAsync(request.UserId);
                if (volunteer == null)
                {
                    return NotFound(new { error = "Volunteer record not found" });
                }

                // Store old role for logging
                var oldRole = volunteer.Role;
                var oldRoleString = oldRole.ToString();
                var newRoleString = request.NewRole.ToString();

                // Update the role in the Volunteer entity
                volunteer.Role = request.NewRole;

                // Update roles in Identity
                await _userManager.RemoveFromRolesAsync(user, new[] { "Volunteer", "Admin", "SuperAdmin" });
                await _userManager.AddToRoleAsync(user, newRoleString);

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role changed for user {UserId} from {OldRole} to {NewRole} by SuperAdmin {AdminId}", 
                    user.Id, oldRoleString, newRoleString, currentUserId);

                return Ok(new 
                { 
                    message = $"User role changed from {oldRoleString} to {newRoleString} successfully",
                    userId = user.Id,
                    userEmail = user.Email,
                    userName = $"{volunteer.FirstName} {volunteer.LastName}",
                    oldRole = oldRoleString,
                    newRole = newRoleString
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role for user {UserId}", request.UserId);
                return StatusCode(500, new { error = "Failed to change user role" });
            }
        }

        [HttpPost("reset-volunteer-password")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ResetVolunteerPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                _logger.LogInformation("Password reset requested by {AdminId} ({Role}) for user {UserId}. CustomPassword provided: {HasCustomPassword}", 
                    currentUserId, currentUserRole, request.VolunteerId, !string.IsNullOrEmpty(request.CustomPassword));

                // Find the user to reset
                var user = await _userManager.FindByIdAsync(request.VolunteerId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new { error = "User not found or inactive" });
                }

                // Prevent users from resetting their own password through this endpoint
                if (user.Id == currentUserId)
                {
                    return BadRequest(new { error = "Cannot reset your own password using this method" });
                }

                // Regular admins can only reset volunteer passwords, not other admin/superadmin passwords
                if (currentUserRole == "Admin" && (user.Role == VolunteerRole.Admin || user.Role == VolunteerRole.SuperAdmin))
                {
                    return Forbid("Admins can only reset volunteer passwords, not other admin or superadmin passwords");
                }

                // Use custom password if provided, otherwise generate a temporary one
                var newPassword = !string.IsNullOrEmpty(request.CustomPassword) 
                    ? request.CustomPassword 
                    : GenerateTemporaryPassword();
                
                _logger.LogInformation("Resetting password for user {UserId}. Using custom password: {UsingCustom}, Password length: {Length}, CustomPassword received: '{CustomPassword}'", 
                    request.VolunteerId, 
                    !string.IsNullOrEmpty(request.CustomPassword), 
                    newPassword.Length,
                    request.CustomPassword ?? "null");
                
                // Validate custom password if provided - using EXACT same rules as Program.cs
                if (!string.IsNullOrEmpty(request.CustomPassword))
                {
                    // These must match exactly with Program.cs identity options
                    if (request.CustomPassword.Length < 6)
                    {
                        return BadRequest(new { error = "Password must be at least 6 characters long" });
                    }
                    
                    if (!request.CustomPassword.Any(char.IsDigit))
                    {
                        return BadRequest(new { error = "Passwords must have at least one digit ('0'-'9')" });
                    }
                    
                    if (!request.CustomPassword.Any(char.IsLower))
                    {
                        return BadRequest(new { error = "Passwords must have at least one lowercase ('a'-'z')" });
                    }
                    
                    // Note: RequireUppercase = false, RequireNonAlphanumeric = false in Program.cs
                    _logger.LogInformation("Custom password validation passed for user {UserId}. Password: digits={HasDigits}, lowercase={HasLower}, uppercase={HasUpper}, special={HasSpecial}, length={Length}", 
                        request.VolunteerId, 
                        request.CustomPassword.Any(char.IsDigit),
                        request.CustomPassword.Any(char.IsLower), 
                        request.CustomPassword.Any(char.IsUpper),
                        request.CustomPassword.Any(c => !char.IsLetterOrDigit(c)),
                        request.CustomPassword.Length);
                }
                
                // For debugging - let's see what we're actually getting
                _logger.LogInformation("Password validation passed. Password contains: digits={HasDigits}, lowercase={HasLower}, length={Length}", 
                    newPassword.Any(char.IsDigit), 
                    newPassword.Any(char.IsLower), 
                    newPassword.Length);

                // First validate the password using UserManager's validators
                var passwordValidators = _userManager.PasswordValidators;
                foreach (var validator in passwordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, user, newPassword);
                    if (!validationResult.Succeeded)
                    {
                        var errors = string.Join(", ", validationResult.Errors.Select(e => e.Description));
                        _logger.LogError("Password validation failed before reset. Errors: {Errors}", errors);
                        return BadRequest(new { error = $"Password validation failed: {errors}" });
                    }
                }

                // Generate a reset token and use it to reset the password
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
                
                if (!resetResult.Succeeded)
                {
                    var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to reset password for user {UserId}. Errors: {Errors}", user.Id, errors);
                    return BadRequest(new { error = $"Failed to reset password: {errors}" });
                }
                
                _logger.LogInformation("Password reset successfully for user {UserId}. Verifying new password...", user.Id);
                
                // Verify the password was set correctly
                var verifyResult = await _userManager.CheckPasswordAsync(user, newPassword);
                if (!verifyResult)
                {
                    _logger.LogError("Password verification failed after reset for user {UserId}", user.Id);
                    return BadRequest(new { error = "Password was reset but verification failed. Please try again." });
                }

                _logger.LogInformation("Password reset successfully for user {Email} ({Role}) by SuperAdmin {AdminId}", 
                    user.Email, user.Role, currentUserId);

                return Ok(new { 
                    success = true,
                    message = $"Password reset successfully for {user.FirstName} {user.LastName}",
                    temporaryPassword = newPassword,
                    volunteerEmail = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting volunteer password");
                return StatusCode(500, new { error = "Failed to reset password" });
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a secure temporary password that meets requirements
            const string lowercase = "abcdefghijkmnpqrstuvwxyz";
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            const string allChars = lowercase + uppercase + digits;
            
            var random = new Random();
            var password = new List<char>();
            
            // Ensure at least one lowercase letter
            password.Add(lowercase[random.Next(lowercase.Length)]);
            
            // Ensure at least one digit
            password.Add(digits[random.Next(digits.Length)]);
            
            // Add remaining characters (6 more for total of 8)
            for (int i = 0; i < 6; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the password
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        [HttpPost("toggle-user-status")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ToggleUserStatus([FromBody] ToggleUserStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                // Find the target user
                var targetUser = await _userManager.FindByIdAsync(request.UserId);
                if (targetUser == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Permission checks
                if (targetUser.Id == currentUserId)
                {
                    return BadRequest(new { error = "Cannot change your own status" });
                }

                // Regular admins can only deactivate volunteers, not other admins/superadmins
                if (currentUserRole == "Admin" && (targetUser.Role == VolunteerRole.Admin || targetUser.Role == VolunteerRole.SuperAdmin))
                {
                    return Forbid("Admins can only activate/deactivate volunteers, not other admins or superadmins");
                }

                // Toggle the user's active status
                var oldStatus = targetUser.IsActive;
                targetUser.IsActive = !targetUser.IsActive;
                
                var result = await _userManager.UpdateAsync(targetUser);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { error = $"Failed to update user status: {errors}" });
                }

                var action = targetUser.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("User {TargetEmail} ({TargetRole}) was {Action} by {AdminEmail} ({AdminRole})", 
                    targetUser.Email, targetUser.Role, action, User.FindFirst(ClaimTypes.Email)?.Value, currentUserRole);

                return Ok(new { 
                    success = true,
                    message = $"User {targetUser.FirstName} {targetUser.LastName} has been {action}",
                    userId = targetUser.Id,
                    newStatus = targetUser.IsActive,
                    userName = $"{targetUser.FirstName} {targetUser.LastName}",
                    userEmail = targetUser.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for user {UserId}", request.UserId);
                return StatusCode(500, new { error = "Failed to update user status" });
            }
        }

        [HttpDelete("delete-user/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                // Find the target user
                var targetUser = await _userManager.FindByIdAsync(userId);
                if (targetUser == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Permission checks
                if (targetUser.Id == currentUserId)
                {
                    return BadRequest(new { error = "Cannot delete your own account" });
                }

                // Check if user has any contacts
                var contactCount = await _context.Contacts
                    .CountAsync(c => c.VolunteerId == userId);
                
                if (contactCount > 0)
                {
                    return BadRequest(new { error = $"Cannot delete user with {contactCount} contact records. Users with contact history cannot be deleted." });
                }

                // Additional safety check - don't delete if this is the only superadmin
                if (targetUser.Role == VolunteerRole.SuperAdmin)
                {
                    var superAdminCount = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                    if (superAdminCount.Count <= 1)
                    {
                        return BadRequest(new { error = "Cannot delete the last SuperAdmin account" });
                    }
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(targetUser);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { error = $"Failed to delete user: {errors}" });
                }

                _logger.LogWarning("User {TargetEmail} ({TargetRole}) was PERMANENTLY DELETED by SuperAdmin {AdminEmail}", 
                    targetUser.Email, targetUser.Role, User.FindFirst(ClaimTypes.Email)?.Value);

                return Ok(new { 
                    success = true,
                    message = $"User {targetUser.FirstName} {targetUser.LastName} has been permanently deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to delete user" });
            }
        }

        [HttpPost("send-engagement-email")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> SendEngagementEmail([FromBody] EngagementEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Subject) || string.IsNullOrEmpty(request.Content))
                {
                    return BadRequest(new { error = "Subject and content are required" });
                }

                // Get recipients based on type
                List<Volunteer> recipients = new();
                
                switch (request.RecipientType.ToLower())
                {
                    case "selected":
                        if (request.SelectedUserIds.Count == 0)
                        {
                            return BadRequest(new { error = "No users selected" });
                        }
                        recipients = await _context.Volunteers
                            .Where(v => request.SelectedUserIds.Contains(v.Id) && v.IsActive)
                            .ToListAsync();
                        break;
                        
                    case "all":
                        recipients = await _context.Volunteers
                            .Where(v => v.IsActive)
                            .ToListAsync();
                        break;
                        
                    case "volunteers":
                        recipients = await _context.Volunteers
                            .Where(v => v.IsActive && v.Role == VolunteerRole.Volunteer)
                            .ToListAsync();
                        break;
                        
                    case "admins":
                        recipients = await _context.Volunteers
                            .Where(v => v.IsActive && (v.Role == VolunteerRole.Admin || v.Role == VolunteerRole.SuperAdmin))
                            .ToListAsync();
                        break;
                        
                    default:
                        return BadRequest(new { error = "Invalid recipient type" });
                }

                if (recipients.Count == 0)
                {
                    return BadRequest(new { error = "No active recipients found" });
                }

                // Get current user info for the email
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = await _userManager.FindByIdAsync(currentUserId!);
                
                var senderName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "Campaign Team";

                // Create HTML content with campaign branding
                var htmlContent = GenerateEngagementEmailHtml(request.Subject, request.Content, senderName);
                var textContent = GenerateEngagementEmailText(request.Content, senderName);

                // Send emails to all recipients
                int successCount = 0;
                var failedEmails = new List<string>();

                foreach (var recipient in recipients)
                {
                    try
                    {
                        var personalizedHtml = htmlContent.Replace("{RecipientName}", recipient.FirstName);
                        var personalizedText = textContent.Replace("{RecipientName}", recipient.FirstName);
                        
                        // Use the existing email service
                        var emailSent = await _emailService.SendEmailAsync(
                            recipient.Email!,
                            request.Subject,
                            personalizedHtml,
                            personalizedText
                        );

                        if (emailSent)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedEmails.Add(recipient.Email!);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send engagement email to {Email}", recipient.Email);
                        failedEmails.Add(recipient.Email!);
                    }
                }

                _logger.LogInformation("Engagement email sent by {SenderName} ({SenderEmail}) to {SuccessCount}/{TotalCount} recipients. Subject: {Subject}",
                    senderName, currentUser?.Email, successCount, recipients.Count, request.Subject);

                var response = new
                {
                    recipientCount = successCount,
                    totalAttempted = recipients.Count,
                    failedCount = failedEmails.Count,
                    message = failedEmails.Count > 0 
                        ? $"Email sent to {successCount} recipients. {failedEmails.Count} failed."
                        : $"Email sent successfully to all {successCount} recipients."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending engagement email");
                return StatusCode(500, new { error = "Failed to send engagement email" });
            }
        }

        private string GenerateEngagementEmailHtml(string subject, string content, string senderName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .greeting {{ font-weight: bold; margin-bottom: 15px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='color: #673ab7; margin: 0;'>Tanveer for Hoover Campaign</h1>
            <p style='color: #666; margin: 5px 0 0 0;'>Campaign Update</p>
        </div>
        
        <div class='content'>
            <div class='greeting'>Hello {{RecipientName}},</div>
            
            <div style='white-space: pre-wrap;'>{content}</div>
            
            <p style='margin-top: 30px;'>Best regards,<br>
            {senderName}<br>
            Tanveer for Hoover Campaign Team</p>
        </div>
        
        <div class='footer'>
            <p>Tanveer Patel for Hoover City Council<br>
            August 26, 2025 Election<br>
            Paid for by Tanveer for Hoover</p>
            
            <p>This is an automated message from the campaign management system.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateEngagementEmailText(string content, string senderName)
        {
            return $@"
Tanveer for Hoover Campaign - Campaign Update

Hello {{RecipientName}},

{content}

Best regards,
{senderName}
Tanveer for Hoover Campaign Team

---
Tanveer Patel for Hoover City Council
August 26, 2025 Election
Paid for by Tanveer for Hoover

This is an automated message from the campaign management system.
";
        }

        // Pending Volunteer Management Endpoints
        [HttpGet("pending-volunteers")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> GetPendingVolunteers()
        {
            try
            {
                var pendingVolunteers = await _context.PendingVolunteers
                    .Where(p => p.Status == PendingVolunteerStatus.Pending)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => new
                    {
                        id = p.Id,
                        firstName = p.FirstName,
                        lastName = p.LastName,
                        email = p.Email,
                        phoneNumber = p.PhoneNumber,
                        requestedRole = p.RequestedRole.ToString(),
                        createdAt = p.CreatedAt,
                        status = p.Status.ToString()
                    })
                    .ToListAsync();

                return Ok(pendingVolunteers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending volunteers");
                return StatusCode(500, new { error = "Failed to fetch pending volunteers" });
            }
        }

        [HttpPost("approve-volunteer/{pendingVolunteerId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ApproveVolunteer(string pendingVolunteerId, [FromBody] ApproveVolunteerRequest? request = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Find pending volunteer
                var pendingVolunteer = await _context.PendingVolunteers
                    .FirstOrDefaultAsync(p => p.Id == pendingVolunteerId && p.Status == PendingVolunteerStatus.Pending);

                if (pendingVolunteer == null)
                {
                    return NotFound(new { error = "Pending volunteer not found" });
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(pendingVolunteer.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "User with this email already exists" });
                }

                // Create new volunteer account
                var newVolunteer = new Volunteer
                {
                    FirstName = pendingVolunteer.FirstName,
                    LastName = pendingVolunteer.LastName,
                    Email = pendingVolunteer.Email,
                    UserName = pendingVolunteer.Email,
                    PhoneNumber = pendingVolunteer.PhoneNumber,
                    Role = pendingVolunteer.RequestedRole,
                    IsActive = true,
                    EmailConfirmed = true
                };

                // Create user account with stored password
                var result = await _userManager.CreateAsync(newVolunteer, "TempPassword123!"); // We'll use the stored hashed password
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { error = $"Failed to create user: {errors}" });
                }

                // Update with the actual hashed password from pending registration
                newVolunteer.PasswordHash = pendingVolunteer.HashedPassword;
                await _userManager.UpdateAsync(newVolunteer);

                // Add user to role
                await _userManager.AddToRoleAsync(newVolunteer, pendingVolunteer.RequestedRole.ToString());

                // Update pending volunteer status
                pendingVolunteer.Status = PendingVolunteerStatus.Approved;
                pendingVolunteer.ReviewedAt = DateTime.UtcNow;
                pendingVolunteer.ReviewedByUserId = currentUserId;
                pendingVolunteer.ReviewNotes = request?.AdminNotes;

                await _context.SaveChangesAsync();

                // Send approval email
                await _emailService.SendRegistrationStatusEmailAsync(
                    pendingVolunteer.Email, 
                    pendingVolunteer.FirstName, 
                    true, 
                    request?.AdminNotes);

                _logger.LogInformation("Volunteer {Email} approved by {AdminId}", pendingVolunteer.Email, currentUserId);

                return Ok(new
                {
                    message = "Volunteer approved successfully",
                    volunteerId = newVolunteer.Id,
                    email = newVolunteer.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving volunteer {VolunteerId}", pendingVolunteerId);
                return StatusCode(500, new { error = "Failed to approve volunteer" });
            }
        }

        [HttpPost("reject-volunteer/{pendingVolunteerId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> RejectVolunteer(string pendingVolunteerId, [FromBody] RejectVolunteerRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Find pending volunteer
                var pendingVolunteer = await _context.PendingVolunteers
                    .FirstOrDefaultAsync(p => p.Id == pendingVolunteerId && p.Status == PendingVolunteerStatus.Pending);

                if (pendingVolunteer == null)
                {
                    return NotFound(new { error = "Pending volunteer not found" });
                }

                // Update pending volunteer status
                pendingVolunteer.Status = PendingVolunteerStatus.Rejected;
                pendingVolunteer.ReviewedAt = DateTime.UtcNow;
                pendingVolunteer.ReviewedByUserId = currentUserId;
                pendingVolunteer.ReviewNotes = request.AdminNotes;

                await _context.SaveChangesAsync();

                // Send rejection email
                await _emailService.SendRegistrationStatusEmailAsync(
                    pendingVolunteer.Email, 
                    pendingVolunteer.FirstName, 
                    false, 
                    request.AdminNotes);

                _logger.LogInformation("Volunteer {Email} rejected by {AdminId}", pendingVolunteer.Email, currentUserId);

                return Ok(new
                {
                    message = "Volunteer registration rejected",
                    email = pendingVolunteer.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting volunteer {VolunteerId}", pendingVolunteerId);
                return StatusCode(500, new { error = "Failed to reject volunteer" });
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
        
        // Login tracking fields
        public int LoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class AnalyticsDto
    {
        public int TotalVoters { get; set; }
        public int TotalContacted { get; set; }
        public ContactStatusBreakdownDto ContactStatusBreakdown { get; set; } = new();
        public List<VolunteerActivityDto> VolunteerActivity { get; set; } = new();
        public List<ContactsByZipDto> ContactsByZip { get; set; } = new();
        
        // Login tracking metrics
        public int TotalLogins { get; set; }
        public int ActiveUsersWeek { get; set; }
        public int ActiveUsersMonth { get; set; }
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

    public class ResetPasswordRequest
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string? CustomPassword { get; set; }
    }

    public class ChangeRoleRequest
    {
        public string UserId { get; set; } = string.Empty;
        public VolunteerRole NewRole { get; set; }
    }

    public class EngagementEmailRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty; // "selected", "all", "volunteers", "admins"
        public List<string> SelectedUserIds { get; set; } = new();
    }

    public class ToggleUserStatusRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class ApproveVolunteerRequest
    {
        public string? AdminNotes { get; set; }
    }

    public class RejectVolunteerRequest
    {
        public string AdminNotes { get; set; } = string.Empty;
    }

    public class LeaderboardResponse
    {
        public List<LeaderboardEntry> WeeklyLeaderboard { get; set; } = new();
        public List<LeaderboardEntry> MonthlyLeaderboard { get; set; } = new();
        public List<Achievement> CurrentUserAchievements { get; set; } = new();
    }

    public class LeaderboardEntry
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string VolunteerName { get; set; } = string.Empty;
        public int ContactCount { get; set; }
        public int Position { get; set; }
        public string Badge { get; set; } = string.Empty;
        public bool IsCurrentUser { get; set; }
    }

    public class Achievement
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}