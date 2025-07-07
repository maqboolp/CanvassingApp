using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalkController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly WalkRoutingService _routingService;
        private readonly IWalkHubService _walkHubService;
        private readonly ILogger<WalkController> _logger;

        public WalkController(
            ApplicationDbContext context,
            WalkRoutingService routingService,
            IWalkHubService walkHubService,
            ILogger<WalkController> logger)
        {
            _context = context;
            _routingService = routingService;
            _walkHubService = walkHubService;
            _logger = logger;
        }

        /// <summary>
        /// Start a new walk session
        /// </summary>
        [HttpPost("sessions/start")]
        public async Task<ActionResult<WalkSessionDto>> StartWalkSession([FromBody] StartWalkSessionRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Check if user already has an active session
            var existingSession = await _context.WalkSessions
                .FirstOrDefaultAsync(s => s.VolunteerId == userId && s.Status == WalkSessionStatus.Active);

            if (existingSession != null)
                return BadRequest(new { error = "You already have an active walk session" });

            var session = new WalkSession
            {
                VolunteerId = userId,
                StartedAt = DateTime.UtcNow,
                Status = WalkSessionStatus.Active,
                StartLatitude = request.Latitude,
                StartLongitude = request.Longitude
            };

            _context.WalkSessions.Add(session);
            await _context.SaveChangesAsync();

            // Log activity
            var activity = new WalkActivity
            {
                WalkSessionId = session.Id,
                ActivityType = WalkActivityType.SessionStarted,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Timestamp = DateTime.UtcNow
            };
            _context.WalkActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(session));
        }

        /// <summary>
        /// Get available houses near current location
        /// </summary>
        [HttpGet("houses/available")]
        public async Task<ActionResult<List<AvailableHouse>>> GetAvailableHouses(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 0.5,
            [FromQuery] int limit = 50)
        {
            var houses = await _routingService.GetAvailableHousesAsync(latitude, longitude, radiusKm, limit);
            return Ok(houses);
        }

        /// <summary>
        /// Generate optimized route through selected houses
        /// </summary>
        [HttpPost("routes/optimize")]
        public async Task<ActionResult<OptimizedRoute>> GenerateOptimizedRoute([FromBody] RouteRequest request)
        {
            if (request.Addresses == null || !request.Addresses.Any())
                return BadRequest(new { error = "No addresses provided" });

            var route = await _routingService.GenerateOptimizedRouteAsync(
                request.StartLatitude,
                request.StartLongitude,
                request.Addresses);

            return Ok(route);
        }

        /// <summary>
        /// Claim houses to prevent duplicate visits
        /// </summary>
        [HttpPost("houses/claim")]
        public async Task<ActionResult<List<HouseClaimDto>>> ClaimHouses([FromBody] ClaimHousesRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Get active session
            var session = await _context.WalkSessions
                .FirstOrDefaultAsync(s => s.VolunteerId == userId && s.Status == WalkSessionStatus.Active);

            if (session == null)
                return BadRequest(new { error = "No active walk session found" });

            var claims = await _routingService.ClaimHousesAsync(
                session.Id,
                request.Addresses,
                request.ClaimDurationMinutes ?? 30);

            // Notify other canvassers about claimed houses
            var volunteerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            foreach (var claim in claims)
            {
                await _walkHubService.NotifyHouseClaimed(
                    claim.Id,
                    claim.Address,
                    int.Parse(userId),
                    volunteerName);
            }

            return Ok(claims.Select(MapToDto));
        }

        /// <summary>
        /// Mark arrival at a house
        /// </summary>
        [HttpPost("houses/{claimId}/arrive")]
        public async Task<ActionResult> ArriveAtHouse(int claimId, [FromBody] LocationUpdate location)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var claim = await _context.HouseClaims
                .Include(c => c.WalkSession)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.WalkSession.VolunteerId == userId);

            if (claim == null)
                return NotFound();

            claim.Status = ClaimStatus.Visiting;
            claim.VisitedAt = DateTime.UtcNow;

            // Log activity
            var activity = new WalkActivity
            {
                WalkSessionId = claim.WalkSessionId,
                ActivityType = WalkActivityType.ArrivedAtHouse,
                HouseClaimId = claimId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Timestamp = DateTime.UtcNow
            };
            _context.WalkActivities.Add(activity);

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Complete visit to a house
        /// </summary>
        [HttpPost("houses/{claimId}/complete")]
        public async Task<ActionResult> CompleteHouseVisit(int claimId, [FromBody] CompleteVisitRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var claim = await _context.HouseClaims
                .Include(c => c.WalkSession)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.WalkSession.VolunteerId == userId);

            if (claim == null)
                return NotFound();

            claim.Status = ClaimStatus.Visited;
            claim.VotersContacted = request.VotersContacted;
            claim.VotersHome = request.VotersHome;
            claim.ContactIds = string.Join(",", request.ContactIds ?? new List<string>());

            // Update session statistics
            claim.WalkSession.HousesVisited++;
            claim.WalkSession.VotersContacted += request.VotersContacted;

            // Log activity
            var activity = new WalkActivity
            {
                WalkSessionId = claim.WalkSessionId,
                ActivityType = WalkActivityType.DepartedHouse,
                HouseClaimId = claimId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Timestamp = DateTime.UtcNow,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    votersContacted = request.VotersContacted,
                    votersHome = request.VotersHome
                })
            };
            _context.WalkActivities.Add(activity);

            await _context.SaveChangesAsync();

            // Notify other canvassers about completed house
            var volunteerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            await _walkHubService.NotifyHouseCompleted(
                claim.Id,
                claim.Address,
                int.Parse(userId),
                volunteerName);

            return Ok();
        }

        /// <summary>
        /// Release a claimed house
        /// </summary>
        [HttpPost("houses/{claimId}/release")]
        public async Task<ActionResult> ReleaseHouse(int claimId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var claim = await _context.HouseClaims
                .Include(c => c.WalkSession)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.WalkSession.VolunteerId == userId);

            if (claim == null)
                return NotFound();

            claim.Status = ClaimStatus.Released;

            // Log activity
            var activity = new WalkActivity
            {
                WalkSessionId = claim.WalkSessionId,
                ActivityType = WalkActivityType.HouseReleased,
                HouseClaimId = claimId,
                Latitude = claim.Latitude,
                Longitude = claim.Longitude,
                Timestamp = DateTime.UtcNow
            };
            _context.WalkActivities.Add(activity);

            await _context.SaveChangesAsync();

            // Notify other canvassers about released house
            await _walkHubService.NotifyHouseReleased(claim.Id, claim.Address);

            return Ok();
        }

        /// <summary>
        /// End current walk session
        /// </summary>
        [HttpPost("sessions/end")]
        public async Task<ActionResult<WalkSessionDto>> EndWalkSession([FromBody] LocationUpdate location)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var session = await _context.WalkSessions
                .Include(s => s.HouseClaims)
                .FirstOrDefaultAsync(s => s.VolunteerId == userId && s.Status == WalkSessionStatus.Active);

            if (session == null)
                return BadRequest(new { error = "No active walk session found" });

            session.Status = WalkSessionStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            session.DurationMinutes = (int)(session.EndedAt.Value - session.StartedAt).TotalMinutes;

            // Release any uncompleted claims
            foreach (var claim in session.HouseClaims.Where(c => c.Status == ClaimStatus.Claimed))
            {
                claim.Status = ClaimStatus.Released;
            }

            // Log activity
            var activity = new WalkActivity
            {
                WalkSessionId = session.Id,
                ActivityType = WalkActivityType.SessionEnded,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Timestamp = DateTime.UtcNow
            };
            _context.WalkActivities.Add(activity);

            await _context.SaveChangesAsync();
            return Ok(MapToDto(session));
        }

        /// <summary>
        /// Get current active session
        /// </summary>
        [HttpGet("sessions/current")]
        public async Task<ActionResult<WalkSessionDto>> GetCurrentSession()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var session = await _context.WalkSessions
                .Include(s => s.HouseClaims)
                .FirstOrDefaultAsync(s => s.VolunteerId == userId && s.Status == WalkSessionStatus.Active);

            if (session == null)
                return NotFound();

            return Ok(MapToDto(session));
        }

        /// <summary>
        /// Get active canvassers in area (for coordination)
        /// </summary>
        [HttpGet("canvassers/active")]
        public async Task<ActionResult<List<ActiveCanvasserDto>>> GetActiveCanvassers(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusKm = 2.0)
        {
            var activeSessions = await _context.WalkSessions
                .Include(s => s.Volunteer)
                .Include(s => s.Activities)
                .Where(s => s.Status == WalkSessionStatus.Active)
                .ToListAsync();

            var activeCanvassers = new List<ActiveCanvasserDto>();

            foreach (var session in activeSessions)
            {
                // Get last known location from activities
                var lastActivity = session.Activities
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefault();

                if (lastActivity != null)
                {
                    var distance = CalculateDistance(
                        latitude, longitude,
                        lastActivity.Latitude, lastActivity.Longitude);

                    if (distance <= radiusKm * 1000)
                    {
                        activeCanvassers.Add(new ActiveCanvasserDto
                        {
                            VolunteerId = session.VolunteerId,
                            Name = $"{session.Volunteer.FirstName} {session.Volunteer.LastName}",
                            LastLatitude = lastActivity.Latitude,
                            LastLongitude = lastActivity.Longitude,
                            LastUpdateTime = lastActivity.Timestamp,
                            HousesVisited = session.HousesVisited,
                            DistanceMeters = distance
                        });
                    }
                }
            }

            return Ok(activeCanvassers.OrderBy(c => c.DistanceMeters));
        }

        /// <summary>
        /// Mark specific voters as contacted during walk
        /// </summary>
        [HttpPost("contact-voters")]
        public async Task<ActionResult> ContactVoters([FromBody] ContactVotersRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Create contacts for each selected voter
                var contacts = new List<Contact>();
                foreach (var voterId in request.VoterIds)
                {
                    var contact = new Contact
                    {
                        VoterId = voterId,
                        VolunteerId = userId,
                        Timestamp = request.ContactedAt ?? DateTime.UtcNow,
                        Status = request.WasHome ? ContactStatus.Reached : ContactStatus.NotHome,
                        Notes = $"Contacted during walk at {request.Address} - {request.ContactMethod ?? "InPerson"}",
                        VoterSupport = VoterSupport.Undecided,
                        LocationLatitude = request.Latitude,
                        LocationLongitude = request.Longitude
                    };
                    
                    _context.Contacts.Add(contact);
                    contacts.Add(contact);
                }

                // Update voter contact status
                var voters = await _context.Voters
                    .Where(v => request.VoterIds.Contains(v.LalVoterId))
                    .ToListAsync();
                
                foreach (var voter in voters)
                {
                    voter.IsContacted = true;
                    voter.LastContactStatus = request.WasHome ? ContactStatus.Reached : ContactStatus.NotHome;
                    voter.LastCampaignContactAt = DateTime.UtcNow;
                    voter.TotalCampaignContacts = voter.TotalCampaignContacts + 1;
                }

                await _context.SaveChangesAsync();

                // Log activity if in active walk session
                var session = await _context.WalkSessions
                    .Include(s => s.Volunteer)
                    .Where(s => s.VolunteerId == userId && s.Status == WalkSessionStatus.Active)
                    .FirstOrDefaultAsync();

                if (session != null)
                {
                    var activity = new WalkActivity
                    {
                        WalkSessionId = session.Id,
                        ActivityType = WalkActivityType.ContactMade,
                        Timestamp = DateTime.UtcNow,
                        Latitude = request.Latitude ?? 0,
                        Longitude = request.Longitude ?? 0,
                        Description = $"Contacted {request.VoterIds.Count} voters at {request.Address}"
                    };
                    _context.WalkActivities.Add(activity);
                    
                    // Update session stats
                    session.HousesVisited = session.HousesVisited + 1;
                    session.VotersContacted = session.VotersContacted + request.VoterIds.Count;
                    
                    await _context.SaveChangesAsync();
                }

                return Ok(new { 
                    success = true, 
                    contactsCreated = contacts.Count,
                    message = $"Successfully marked {contacts.Count} voters as contacted" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking voters as contacted");
                return StatusCode(500, new { error = "Failed to mark voters as contacted", details = ex.Message });
            }
        }

        // Helper methods
        private WalkSessionDto MapToDto(WalkSession session)
        {
            return new WalkSessionDto
            {
                Id = session.Id,
                VolunteerId = session.VolunteerId,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                Status = session.Status.ToString().ToLower(),
                HousesVisited = session.HousesVisited,
                VotersContacted = session.VotersContacted,
                TotalDistanceMeters = session.TotalDistanceMeters,
                DurationMinutes = session.DurationMinutes,
                ActiveClaims = session.HouseClaims?
                    .Where(c => c.Status == ClaimStatus.Claimed || c.Status == ClaimStatus.Visiting)
                    .Select(MapToDto)
                    .ToList() ?? new List<HouseClaimDto>()
            };
        }

        private HouseClaimDto MapToDto(HouseClaim claim)
        {
            return new HouseClaimDto
            {
                Id = claim.Id,
                Address = claim.Address,
                Latitude = claim.Latitude,
                Longitude = claim.Longitude,
                ClaimedAt = claim.ClaimedAt,
                ExpiresAt = claim.ExpiresAt,
                Status = claim.Status.ToString().ToLower(),
                VisitedAt = claim.VisitedAt
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth's radius in meters
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }

    // Request/Response DTOs
    public class StartWalkSessionRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RouteRequest
    {
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public List<string> Addresses { get; set; } = new();
    }

    public class ClaimHousesRequest
    {
        public List<string> Addresses { get; set; } = new();
        public int? ClaimDurationMinutes { get; set; } = 30;
    }

    public class LocationUpdate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CompleteVisitRequest : LocationUpdate
    {
        public int VotersContacted { get; set; }
        public int VotersHome { get; set; }
        public List<string>? ContactIds { get; set; }
    }

    public class WalkSessionDto
    {
        public int Id { get; set; }
        public string VolunteerId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int HousesVisited { get; set; }
        public int VotersContacted { get; set; }
        public double TotalDistanceMeters { get; set; }
        public int DurationMinutes { get; set; }
        public List<HouseClaimDto> ActiveClaims { get; set; } = new();
    }

    public class HouseClaimDto
    {
        public int Id { get; set; }
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ClaimedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? VisitedAt { get; set; }
    }

    public class ActiveCanvasserDto
    {
        public string VolunteerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double LastLatitude { get; set; }
        public double LastLongitude { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public int HousesVisited { get; set; }
        public double DistanceMeters { get; set; }
    }

    public class ContactVotersRequest
    {
        public string Address { get; set; } = string.Empty;
        public List<string> VoterIds { get; set; } = new();
        public DateTime? ContactedAt { get; set; }
        public bool WasHome { get; set; } = true;
        public string? ContactMethod { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}