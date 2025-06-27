using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Security.Claims;
using System.Linq;

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
            [FromQuery] double radiusKm = 1.0,
            [FromQuery] string? partyAffiliation = null,
            [FromQuery] List<int>? tagIds = null)
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

                if (!string.IsNullOrEmpty(partyAffiliation))
                {
                    query = query.Where(v => v.PartyAffiliation == partyAffiliation);
                }

                // Filter by tags
                if (tagIds != null && tagIds.Any())
                {
                    query = query.Where(v => v.TagAssignments.Any(ta => tagIds.Contains(ta.TagId)));
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
                    .Include(v => v.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
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
                            PartyAffiliation = v.PartyAffiliation,
                            CellPhone = v.CellPhone,
                            Email = v.Email,
                            Latitude = v.Latitude,
                            Longitude = v.Longitude,
                            IsContacted = v.IsContacted,
                            LastContactStatus = v.LastContactStatus?.ToString().ToLower(),
                            Tags = v.TagAssignments.Select(ta => new TagDto
                            {
                                Id = ta.Tag.Id,
                                TagName = ta.Tag.TagName,
                                Color = ta.Tag.Color
                            }).ToList(),
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
                    .Include(v => v.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
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
                    PartyAffiliation = voter.PartyAffiliation,
                    CellPhone = voter.CellPhone,
                    Email = voter.Email,
                    Latitude = voter.Latitude,
                    Longitude = voter.Longitude,
                    IsContacted = voter.IsContacted,
                    LastContactStatus = voter.LastContactStatus?.ToString().ToLower(),
                    VoterSupport = voter.VoterSupport?.ToString().ToLower(),
                    Tags = voter.TagAssignments.Select(ta => new TagDto
                    {
                        Id = ta.Tag.Id,
                        TagName = ta.Tag.TagName,
                        Color = ta.Tag.Color
                    }).ToList()
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
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                _logger.LogInformation("GetNearestVoter called by user {UserId} with role {Role} at location {Lat},{Lng}", 
                    currentUserId, currentUserRole, latitude, longitude);
                
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

                _logger.LogInformation("Found {Count} uncontacted voters with coordinates for user {UserId}", 
                    voters.Count, currentUserId);

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
                    _logger.LogInformation("No uncontacted voters found within {Distance}km for user {UserId}", 
                        maxDistanceKm, currentUserId);
                    return NotFound(new { message = $"No uncontacted voters found within {maxDistanceKm}km" });
                }

                _logger.LogInformation("Found nearest voter at distance {Distance}km for user {UserId}", 
                    votersWithDistance.Distance, currentUserId);

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
                    PartyAffiliation = voter.PartyAffiliation,
                    CellPhone = voter.CellPhone,
                    Email = voter.Email,
                    Latitude = voter.Latitude,
                    Longitude = voter.Longitude,
                    IsContacted = voter.IsContacted,
                    LastContactStatus = voter.LastContactStatus?.ToString().ToLower(),
                    VoterSupport = voter.VoterSupport?.ToString().ToLower(),
                    Tags = voter.TagAssignments.Select(ta => new TagDto
                    {
                        Id = ta.Tag.Id,
                        TagName = ta.Tag.TagName,
                        Color = ta.Tag.Color
                    }).ToList()
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

        [HttpGet("debug-stats")]
        public async Task<ActionResult> GetDebugStats()
        {
            try
            {
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                // Only allow admin/superadmin to access debug stats
                if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
                {
                    return Forbid();
                }

                var totalVoters = await _context.Voters.CountAsync();
                var votersWithCoords = await _context.Voters
                    .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                    .CountAsync();
                var uncontactedVoters = await _context.Voters
                    .Where(v => !v.IsContacted)
                    .CountAsync();
                var uncontactedWithCoords = await _context.Voters
                    .Where(v => !v.IsContacted && v.Latitude.HasValue && v.Longitude.HasValue)
                    .CountAsync();

                return Ok(new {
                    totalVoters,
                    votersWithCoordinates = votersWithCoords,
                    uncontactedVoters,
                    uncontactedVotersWithCoordinates = uncontactedWithCoords,
                    message = "Debug stats for troubleshooting nearest voter issue"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debug stats");
                return StatusCode(500, new { error = "Failed to get debug stats" });
            }
        }

        // POST: api/voters/{id}/tags
        [HttpPost("{id}/tags")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddTagsToVoter(string id, [FromBody] AddTagsRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                var voter = await _context.Voters.FindAsync(id);
                if (voter == null)
                {
                    return NotFound(new { error = "Voter not found" });
                }

                // Get existing assignments to avoid duplicates
                var existingTagIds = await _context.VoterTagAssignments
                    .Where(vta => vta.VoterId == id && request.TagIds.Contains(vta.TagId))
                    .Select(vta => vta.TagId)
                    .ToListAsync();

                var newTagIds = request.TagIds.Except(existingTagIds).ToList();

                if (newTagIds.Any())
                {
                    var assignments = newTagIds.Select(tagId => new VoterTagAssignment
                    {
                        VoterId = id,
                        TagId = tagId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedById = currentUserId
                    });

                    _context.VoterTagAssignments.AddRange(assignments);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { addedCount = newTagIds.Count, skippedCount = existingTagIds.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tags to voter {VoterId}", id);
                return StatusCode(500, new { error = "Failed to add tags" });
            }
        }

        // DELETE: api/voters/{id}/tags
        [HttpDelete("{id}/tags")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RemoveTagsFromVoter(string id, [FromBody] RemoveTagsRequest request)
        {
            try
            {
                var assignments = await _context.VoterTagAssignments
                    .Where(vta => vta.VoterId == id && request.TagIds.Contains(vta.TagId))
                    .ToListAsync();

                if (assignments.Any())
                {
                    _context.VoterTagAssignments.RemoveRange(assignments);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { removedCount = assignments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tags from voter {VoterId}", id);
                return StatusCode(500, new { error = "Failed to remove tags" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<VoterDto>> CreateVoter(CreateVoterRequest request)
        {
            try
            {
                // Generate a unique voter ID
                var voterId = $"MAN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
                
                // Format phone number if provided
                string? formattedPhone = null;
                if (!string.IsNullOrEmpty(request.CellPhone))
                {
                    var digits = new string(request.CellPhone.Where(char.IsDigit).ToArray());
                    if (digits.Length == 10)
                    {
                        formattedPhone = $"+1{digits}";
                    }
                    else if (digits.Length == 11 && digits.StartsWith("1"))
                    {
                        formattedPhone = $"+{digits}";
                    }
                    else
                    {
                        formattedPhone = request.CellPhone;
                    }
                }

                var voter = new Voter
                {
                    LalVoterId = voterId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    AddressLine = request.AddressLine,
                    City = request.City,
                    State = request.State,
                    Zip = request.Zip,
                    Age = request.Age,
                    Gender = request.Gender ?? "Unknown",
                    VoteFrequency = request.VoteFrequency ?? VoteFrequency.NonVoter,
                    PartyAffiliation = request.PartyAffiliation,
                    CellPhone = formattedPhone,
                    Email = request.Email,
                    IsContacted = false,
                    SmsConsentStatus = SmsConsentStatus.Unknown,
                    TotalCampaignContacts = 0,
                    SmsCount = 0,
                    CallCount = 0
                };

                _context.Voters.Add(voter);
                await _context.SaveChangesAsync();

                var createdVoter = await _context.Voters
                    .Include(v => v.TagAssignments)
                    .ThenInclude(ta => ta.Tag)
                    .FirstOrDefaultAsync(v => v.LalVoterId == voterId);

                var voterDto = new VoterDto
                {
                    LalVoterId = createdVoter!.LalVoterId,
                    FirstName = createdVoter.FirstName,
                    MiddleName = createdVoter.MiddleName,
                    LastName = createdVoter.LastName,
                    AddressLine = createdVoter.AddressLine,
                    City = createdVoter.City,
                    State = createdVoter.State,
                    Zip = createdVoter.Zip,
                    Age = createdVoter.Age,
                    Ethnicity = createdVoter.Ethnicity,
                    Gender = createdVoter.Gender,
                    VoteFrequency = createdVoter.VoteFrequency.ToString().ToLower(),
                    PartyAffiliation = createdVoter.PartyAffiliation,
                    CellPhone = createdVoter.CellPhone,
                    Email = createdVoter.Email,
                    Latitude = createdVoter.Latitude,
                    Longitude = createdVoter.Longitude,
                    IsContacted = createdVoter.IsContacted,
                    LastContactStatus = createdVoter.LastContactStatus?.ToString().ToLower(),
                    VoterSupport = createdVoter.VoterSupport?.ToString().ToLower(),
                    Tags = new List<TagDto>()
                };

                _logger.LogInformation("Voter {VoterId} created manually by user {UserId}", voterId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                return CreatedAtAction(nameof(GetVoter), new { id = voter.LalVoterId }, voterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voter");
                return StatusCode(500, new { error = "Failed to create voter" });
            }
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
        public string? PartyAffiliation { get; set; }
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsContacted { get; set; }
        public string? LastContactStatus { get; set; }
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
        public string? VoterSupport { get; set; }
        public double? DistanceKm { get; set; }
    }

    public class TagDto
    {
        public int Id { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    public class VoterListResponse
    {
        public List<VoterDto> Voters { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class AddTagsRequest
    {
        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class RemoveTagsRequest
    {
        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class CreateVoterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Gender { get; set; }
        public VoteFrequency? VoteFrequency { get; set; }
        public string? PartyAffiliation { get; set; }
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
    }
}