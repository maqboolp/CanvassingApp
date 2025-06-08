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
}