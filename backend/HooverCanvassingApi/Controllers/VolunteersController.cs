using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Volunteer,Admin,SuperAdmin")]
    public class VolunteersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VolunteersController> _logger;

        public VolunteersController(ApplicationDbContext context, ILogger<VolunteersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("leaderboard")]
        public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Get this week's date range
                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);

                // Get this month's date range
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Get all active volunteers first
                var volunteers = await _context.Volunteers
                    .Where(v => v.IsActive)
                    .ToListAsync();

                // Calculate weekly leaderboard in memory to avoid EF issues
                var weeklyLeaderboard = volunteers
                    .Select(v => new LeaderboardEntry
                    {
                        VolunteerId = v.Id,
                        VolunteerName = $"{v.FirstName} {v.LastName}",
                        ContactCount = _context.Contacts.Count(c => c.VolunteerId == v.Id && c.Timestamp >= weekStart && c.Timestamp < weekEnd),
                        IsCurrentUser = v.Id == currentUserId
                    })
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
                        ContactCount = _context.Contacts.Count(c => c.VolunteerId == v.Id && c.Timestamp >= monthStart && c.Timestamp < monthEnd),
                        IsCurrentUser = v.Id == currentUserId
                    })
                    .OrderByDescending(v => v.ContactCount)
                    .ThenBy(v => v.VolunteerName)
                    .Take(10)
                    .ToList();

                // Assign positions and badges
                AssignPositionsAndBadges(weeklyLeaderboard);
                AssignPositionsAndBadges(monthlyLeaderboard);

                // Get current user's achievements
                var currentUser = await _context.Volunteers
                    .Include(v => v.Contacts)
                    .FirstOrDefaultAsync(v => v.Id == currentUserId);

                var achievements = new List<Achievement>();
                if (currentUser != null)
                {
                    var totalContacts = currentUser.Contacts.Count;
                    var todayContacts = currentUser.Contacts.Count(c => c.Timestamp.Date == today);
                    var weekContacts = currentUser.Contacts.Count(c => c.Timestamp >= weekStart && c.Timestamp < weekEnd);

                    // Add achievement badges based on milestones
                    if (totalContacts >= 100) achievements.Add(new Achievement { Name = "Century Club", Description = "100+ Total Contacts", Icon = "🏆" });
                    else if (totalContacts >= 50) achievements.Add(new Achievement { Name = "Half Century", Description = "50+ Total Contacts", Icon = "🥉" });
                    else if (totalContacts >= 25) achievements.Add(new Achievement { Name = "Quarter Century", Description = "25+ Total Contacts", Icon = "🎖️" });
                    else if (totalContacts >= 10) achievements.Add(new Achievement { Name = "Getting Started", Description = "10+ Total Contacts", Icon = "⭐" });

                    if (todayContacts >= 10) achievements.Add(new Achievement { Name = "Daily Champion", Description = "10+ Contacts Today", Icon = "🔥" });
                    if (weekContacts >= 25) achievements.Add(new Achievement { Name = "Weekly Warrior", Description = "25+ Contacts This Week", Icon = "💪" });
                }

                var response = new LeaderboardResponse
                {
                    WeeklyLeaderboard = weeklyLeaderboard,
                    MonthlyLeaderboard = monthlyLeaderboard,
                    CurrentUserAchievements = achievements
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leaderboard for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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

        [HttpGet("stats")]
        public async Task<ActionResult<VolunteerStatsResponse>> GetVolunteerStats()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Get volunteer's contact statistics
                var totalContacted = await _context.Contacts
                    .Where(c => c.VolunteerId == currentUserId)
                    .CountAsync();

                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);
                var contactsToday = await _context.Contacts
                    .Where(c => c.VolunteerId == currentUserId && 
                               c.Timestamp >= todayStart && c.Timestamp < todayEnd)
                    .CountAsync();

                // For now, since we removed assignments, we'll count nearby voters within a reasonable distance
                // This gives volunteers a sense of how many voters are in their area
                var totalNearbyVoters = await _context.Voters
                    .Where(v => !v.IsContacted)
                    .CountAsync();

                var stats = new VolunteerStatsResponse
                {
                    TotalAssigned = totalNearbyVoters, // Available voters in the area
                    Contacted = totalContacted,
                    ContactsToday = contactsToday
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching volunteer stats for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { error = "Failed to fetch volunteer stats" });
            }
        }
    }

    public class VolunteerStatsResponse
    {
        public int TotalAssigned { get; set; }
        public int Contacted { get; set; }
        public int ContactsToday { get; set; }
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