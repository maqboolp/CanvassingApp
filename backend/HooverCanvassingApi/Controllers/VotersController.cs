using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Volunteer,Admin,SuperAdmin")]
    public class VotersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VotersController> _logger;

        public VotersController(ApplicationDbContext context, ILogger<VotersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<VoterListResponse>> GetVoters(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 25,
            [FromQuery] string? zipCode = null,
            [FromQuery] string? voteFrequency = null,
            [FromQuery] string? ageGroup = null,
            [FromQuery] string? contactStatus = null,
            [FromQuery] string? searchName = null,
            [FromQuery] bool assignedToMe = false,
            [FromQuery] string? sortBy = "lastName",
            [FromQuery] string? sortOrder = "asc",
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double radiusKm = 1.0)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var query = _context.Voters.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchName))
                {
                    query = query.Where(v => 
                        v.FirstName.Contains(searchName) || 
                        v.LastName.Contains(searchName) ||
                        (v.FirstName + " " + v.LastName).Contains(searchName));
                }

                if (!string.IsNullOrEmpty(zipCode))
                {
                    query = query.Where(v => v.Zip == zipCode);
                }

                if (!string.IsNullOrEmpty(voteFrequency) && Enum.TryParse<VoteFrequency>(voteFrequency, true, out var vf))
                {
                    query = query.Where(v => v.VoteFrequency == vf);
                }

                if (!string.IsNullOrEmpty(ageGroup))
                {
                    switch (ageGroup.ToLower())
                    {
                        case "18-30":
                            query = query.Where(v => v.Age >= 18 && v.Age <= 30);
                            break;
                        case "31-50":
                            query = query.Where(v => v.Age >= 31 && v.Age <= 50);
                            break;
                        case "51+":
                            query = query.Where(v => v.Age >= 51);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(contactStatus))
                {
                    switch (contactStatus.ToLower())
                    {
                        case "contacted":
                            query = query.Where(v => v.IsContacted);
                            break;
                        case "not-contacted":
                            query = query.Where(v => !v.IsContacted);
                            break;
                    }
                }

                // Apply location filtering if coordinates are provided
                // For now, we'll just filter to voters with coordinates and do distance filtering in memory
                if (latitude.HasValue && longitude.HasValue)
                {
                    query = query.Where(v => v.Latitude.HasValue && v.Longitude.HasValue);
                }

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "firstname" => sortOrder?.ToLower() == "desc" 
                        ? query.OrderByDescending(v => v.FirstName)
                        : query.OrderBy(v => v.FirstName),
                    "age" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(v => v.Age)
                        : query.OrderBy(v => v.Age),
                    "zip" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(v => v.Zip)
                        : query.OrderBy(v => v.Zip),
                    "votefrequency" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(v => v.VoteFrequency)
                        : query.OrderBy(v => v.VoteFrequency),
                    _ => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(v => v.LastName)
                        : query.OrderBy(v => v.LastName)
                };

                var voters = await query
                    .ToListAsync();

                // Apply distance filtering and sorting in memory if location is provided
                List<(Voter Voter, double Distance)> votersWithDistance = null;
                if (latitude.HasValue && longitude.HasValue)
                {
                    votersWithDistance = voters
                        .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                        .Select(v => (
                            Voter: v,
                            Distance: CalculateDistance(latitude.Value, longitude.Value, v.Latitude!.Value, v.Longitude!.Value)
                        ))
                        .Where(vd => vd.Distance <= radiusKm)
                        .OrderBy(vd => vd.Distance) // Sort by distance from closest to farthest
                        .ToList();
                    
                    voters = votersWithDistance.Select(vd => vd.Voter).ToList();
                }

                var total = voters.Count;
                
                // Apply pagination after filtering
                voters = voters
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                var response = new VoterListResponse
                {
                    Voters = voters.Select(v => 
                    {
                        // Find distance for this voter if location search was used
                        double? distance = null;
                        if (votersWithDistance != null && latitude.HasValue && longitude.HasValue)
                        {
                            var voterWithDist = votersWithDistance.FirstOrDefault(vd => vd.Voter.LalVoterId == v.LalVoterId);
                            distance = voterWithDist.Distance;
                        }
                        
                        return new VoterDto
                        {
                            LalVoterId = v.LalVoterId,
                            FirstName = v.FirstName,
                            MiddleName = v.MiddleName,
                            LastName = v.LastName,
                            AddressLine = v.AddressLine,
                            City = v.City,
                            State = v.State,
                            Zip = v.Zip,
                            Age = v.Age,
                            Ethnicity = v.Ethnicity,
                            Gender = v.Gender,
                            VoteFrequency = v.VoteFrequency.ToString().ToLower(),
                            CellPhone = v.CellPhone,
                            Email = v.Email,
                            Latitude = v.Latitude,
                            Longitude = v.Longitude,
                            IsContacted = v.IsContacted,
                            LastContactStatus = v.LastContactStatus?.ToString().ToLower(),
                            VoterSupport = v.VoterSupport?.ToString().ToLower(),
                            DistanceKm = distance
                        };
                    }).ToList(),
                    Total = total,
                    Page = page,
                    TotalPages = (int)Math.Ceiling((double)total / limit)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voters");
                return StatusCode(500, new { error = "Failed to retrieve voters" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VoterDto>> GetVoter(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var voter = await _context.Voters
                    .Include(v => v.Contacts.OrderByDescending(c => c.Timestamp))
                    .FirstOrDefaultAsync(v => v.LalVoterId == id);

                if (voter == null)
                {
                    return NotFound();
                }

                var voterDto = new VoterDto
                {
                    LalVoterId = voter.LalVoterId,
                    FirstName = voter.FirstName,
                    MiddleName = voter.MiddleName,
                    LastName = voter.LastName,
                    AddressLine = voter.AddressLine,
                    City = voter.City,
                    State = voter.State,
                    Zip = voter.Zip,
                    Age = voter.Age,
                    Ethnicity = voter.Ethnicity,
                    Gender = voter.Gender,
                    VoteFrequency = voter.VoteFrequency.ToString().ToLower(),
                    CellPhone = voter.CellPhone,
                    Email = voter.Email,
                    Latitude = voter.Latitude,
                    Longitude = voter.Longitude,
                    IsContacted = voter.IsContacted,
                    LastContactStatus = voter.LastContactStatus?.ToString().ToLower(),
                    VoterSupport = voter.VoterSupport?.ToString().ToLower()
                };

                return Ok(voterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voter {VoterId}", id);
                return StatusCode(500, new { error = "Failed to retrieve voter" });
            }
        }

        [HttpGet("nearest")]
        public async Task<ActionResult<VoterDto>> GetNearestVoter(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double maxDistanceKm = 10.0)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // Get voters who haven't been contacted
                var voters = await _context.Voters
                    .Where(v => !v.IsContacted &&
                               v.Latitude.HasValue &&
                               v.Longitude.HasValue)
                    .ToListAsync();

                if (!voters.Any())
                {
                    return NotFound(new { message = "No uncontacted voters found" });
                }

                // Calculate distances and find nearest
                var votersWithDistance = voters
                    .Select(v => new
                    {
                        Voter = v,
                        Distance = CalculateDistance(latitude, longitude, v.Latitude!.Value, v.Longitude!.Value)
                    })
                    .Where(vd => vd.Distance <= maxDistanceKm)
                    .OrderBy(vd => vd.Distance)
                    .FirstOrDefault();

                if (votersWithDistance == null)
                {
                    return NotFound(new { message = $"No uncontacted voters found within {maxDistanceKm}km" });
                }

                var voter = votersWithDistance.Voter;
                var voterDto = new VoterDto
                {
                    LalVoterId = voter.LalVoterId,
                    FirstName = voter.FirstName,
                    MiddleName = voter.MiddleName,
                    LastName = voter.LastName,
                    AddressLine = voter.AddressLine,
                    City = voter.City,
                    State = voter.State,
                    Zip = voter.Zip,
                    Age = voter.Age,
                    Ethnicity = voter.Ethnicity,
                    Gender = voter.Gender,
                    VoteFrequency = voter.VoteFrequency.ToString().ToLower(),
                    CellPhone = voter.CellPhone,
                    Email = voter.Email,
                    Latitude = voter.Latitude,
                    Longitude = voter.Longitude,
                    IsContacted = voter.IsContacted,
                    LastContactStatus = voter.LastContactStatus?.ToString().ToLower(),
                    VoterSupport = voter.VoterSupport?.ToString().ToLower()
                };

                return Ok(new { voter = voterDto, distance = votersWithDistance.Distance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding nearest voter");
                return StatusCode(500, new { error = "Failed to find nearest voter" });
            }
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // km

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    public class VoterDto
    {
        public string LalVoterId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Ethnicity { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string VoteFrequency { get; set; } = string.Empty;
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsContacted { get; set; }
        public string? LastContactStatus { get; set; }
        public string? VoterSupport { get; set; }
        public double? DistanceKm { get; set; }
    }

    public class VoterListResponse
    {
        public List<VoterDto> Voters { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}