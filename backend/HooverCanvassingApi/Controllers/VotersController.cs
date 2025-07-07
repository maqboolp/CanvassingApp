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
                    var searchLower = searchName.ToLower();
                    query = query.Where(v => 
                        v.FirstName.ToLower().Contains(searchLower) || 
                        v.LastName.ToLower().Contains(searchLower) ||
                        (v.FirstName + " " + v.LastName).ToLower().Contains(searchLower) ||
                        v.AddressLine.ToLower().Contains(searchLower) ||
                        v.City.ToLower().Contains(searchLower) ||
                        v.Zip.Contains(searchName)); // Use original case for ZIP code
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
                    var stats = new {
                        message = "No uncontacted voters with coordinates found",
                        totalVoters = await _context.Voters.CountAsync(),
                        votersWithCoordinates = await _context.Voters.CountAsync(v => v.Latitude.HasValue && v.Longitude.HasValue),
                        uncontactedVoters = await _context.Voters.CountAsync(v => !v.IsContacted),
                        uncontactedWithCoordinates = 0
                    };
                    _logger.LogInformation("No uncontacted voters with coordinates found. Stats: {@Stats}", stats);
                    return NotFound(stats);
                }

                // Calculate distances for all voters
                var allVotersWithDistance = voters
                    .Select(v => new
                    {
                        Voter = v,
                        Distance = CalculateDistance(latitude, longitude, v.Latitude!.Value, v.Longitude!.Value)
                    })
                    .OrderBy(vd => vd.Distance)
                    .ToList();

                // Find nearest within max distance
                var nearestWithinRange = allVotersWithDistance
                    .FirstOrDefault(vd => vd.Distance <= maxDistanceKm);

                if (nearestWithinRange == null)
                {
                    _logger.LogInformation("No uncontacted voters found within {Distance}km for user {UserId}", 
                        maxDistanceKm, currentUserId);
                    
                    var nearestDistance = allVotersWithDistance.Any() ? allVotersWithDistance.First().Distance : 0;
                    return NotFound(new { 
                        message = $"No uncontacted voters found within {maxDistanceKm}km",
                        nearestVoterDistance = nearestDistance,
                        totalUncontactedWithCoordinates = voters.Count,
                        suggestion = nearestDistance > 0 ? $"Try increasing the search radius. Nearest voter is {nearestDistance:F1}km away" : null
                    });
                }

                _logger.LogInformation("Found nearest voter at distance {Distance}km for user {UserId}", 
                    nearestWithinRange.Distance, currentUserId);

                var voter = nearestWithinRange.Voter;
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

                return Ok(new { voter = voterDto, distance = nearestWithinRange.Distance });
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

        [HttpGet("check-geocoding")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> CheckGeocodingAccuracy([FromQuery] string? addressPattern = null)
        {
            try
            {
                // Default to checking addresses with "1112" if no pattern provided
                var searchPattern = addressPattern ?? "1112";
                
                // Query voters with addresses containing the pattern
                var votersQuery = _context.Voters
                    .Where(v => v.AddressLine.Contains(searchPattern) && 
                               v.Latitude.HasValue && 
                               v.Longitude.HasValue);

                var voters = await votersQuery
                    .Select(v => new
                    {
                        v.LalVoterId,
                        v.FirstName,
                        v.LastName,
                        v.AddressLine,
                        v.City,
                        v.State,
                        v.Zip,
                        v.Latitude,
                        v.Longitude
                    })
                    .ToListAsync();

                // Analyze the coordinates for potential water body locations
                var suspiciousVoters = new List<object>();
                
                foreach (var voter in voters)
                {
                    // Check if coordinates might be in water bodies
                    // This is a simplified check - you might want to integrate with a geocoding API
                    // to verify if the coordinates are actually on land
                    
                    // For Alabama (Hoover area), typical residential coordinates would be:
                    // Latitude: around 33.3-33.5
                    // Longitude: around -86.7 to -86.9
                    
                    // Flag voters with unusual coordinates or those that might be in water
                    var isSuspicious = false;
                    var reasons = new List<string>();
                    
                    // Check if latitude/longitude are within expected range for Hoover, AL area
                    if (voter.Latitude < 33.0 || voter.Latitude > 34.0)
                    {
                        isSuspicious = true;
                        reasons.Add($"Latitude {voter.Latitude} is outside expected range for Hoover area (33.0-34.0)");
                    }
                    
                    if (voter.Longitude < -87.5 || voter.Longitude > -86.0)
                    {
                        isSuspicious = true;
                        reasons.Add($"Longitude {voter.Longitude} is outside expected range for Hoover area (-87.5 to -86.0)");
                    }
                    
                    // Check for coordinates that might be default/error values
                    if (Math.Abs(voter.Latitude.Value) < 1 || Math.Abs(voter.Longitude.Value) < 1)
                    {
                        isSuspicious = true;
                        reasons.Add("Coordinates appear to be near 0,0 which suggests geocoding error");
                    }
                    
                    // Check if coordinates have too many decimal places (might indicate precision issues)
                    var latDecimals = BitConverter.GetBytes(decimal.GetBits((decimal)voter.Latitude.Value)[3])[2];
                    var lonDecimals = BitConverter.GetBytes(decimal.GetBits((decimal)voter.Longitude.Value)[3])[2];
                    
                    if (latDecimals < 4 || lonDecimals < 4)
                    {
                        isSuspicious = true;
                        reasons.Add($"Low coordinate precision (lat: {latDecimals} decimals, lon: {lonDecimals} decimals)");
                    }
                    
                    // Add to suspicious list if any issues found
                    if (isSuspicious || addressPattern != null)
                    {
                        suspiciousVoters.Add(new
                        {
                            voter.LalVoterId,
                            FullName = $"{voter.FirstName} {voter.LastName}",
                            voter.AddressLine,
                            voter.City,
                            voter.State,
                            voter.Zip,
                            voter.Latitude,
                            voter.Longitude,
                            GoogleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={voter.Latitude},{voter.Longitude}",
                            IsSuspicious = isSuspicious,
                            Reasons = reasons,
                            FullAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}"
                        });
                    }
                }
                
                // Get summary statistics
                var stats = new
                {
                    TotalVotersWithPattern = voters.Count,
                    SuspiciousCount = suspiciousVoters.Count(v => ((dynamic)v).IsSuspicious),
                    SearchPattern = searchPattern,
                    VotersChecked = suspiciousVoters,
                    Summary = new
                    {
                        TotalInDatabase = await _context.Voters.CountAsync(),
                        WithCoordinates = await _context.Voters.CountAsync(v => v.Latitude.HasValue && v.Longitude.HasValue),
                        WithoutCoordinates = await _context.Voters.CountAsync(v => !v.Latitude.HasValue || !v.Longitude.HasValue)
                    }
                };
                
                _logger.LogInformation("Geocoding check completed for pattern '{Pattern}'. Found {Total} voters, {Suspicious} suspicious", 
                    searchPattern, voters.Count, stats.SuspiciousCount);
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking geocoding accuracy");
                return StatusCode(500, new { error = "Failed to check geocoding accuracy" });
            }
        }

        [HttpGet("water-body-check")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> CheckWaterBodyGeocoding([FromQuery] int limit = 100)
        {
            try
            {
                // Known water body areas in the Hoover/Birmingham area
                // These are approximate coordinate ranges for lakes and rivers
                var waterBodyRanges = new[]
                {
                    new { Name = "Lake Purdy", MinLat = 33.420, MaxLat = 33.460, MinLon = -86.680, MaxLon = -86.620 },
                    new { Name = "Cahaba River area", MinLat = 33.300, MaxLat = 33.500, MinLon = -86.850, MaxLon = -86.750 },
                    new { Name = "Black Creek area", MinLat = 33.350, MaxLat = 33.450, MinLon = -86.820, MaxLon = -86.780 }
                };

                var potentialWaterVoters = new List<object>();

                // Get all voters with coordinates
                var votersWithCoords = await _context.Voters
                    .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                    .Select(v => new
                    {
                        v.LalVoterId,
                        v.FirstName,
                        v.LastName,
                        v.AddressLine,
                        v.City,
                        v.State,
                        v.Zip,
                        v.Latitude,
                        v.Longitude
                    })
                    .Take(limit)
                    .ToListAsync();

                foreach (var voter in votersWithCoords)
                {
                    var waterBodyMatches = new List<string>();
                    
                    // Check if voter coordinates fall within any water body range
                    foreach (var waterBody in waterBodyRanges)
                    {
                        if (voter.Latitude >= waterBody.MinLat && voter.Latitude <= waterBody.MaxLat &&
                            voter.Longitude >= waterBody.MinLon && voter.Longitude <= waterBody.MaxLon)
                        {
                            waterBodyMatches.Add(waterBody.Name);
                        }
                    }

                    // Also check for addresses that commonly get miscoded
                    var addressIndicators = new[]
                    {
                        "1112", "1111", "1113", "1114", "1110",
                        "Lake", "River", "Creek", "Pond", "Water",
                        "Bridge", "Dam", "Shore", "Beach", "Marina"
                    };

                    var addressMatches = addressIndicators
                        .Where(indicator => voter.AddressLine.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (waterBodyMatches.Any() || addressMatches.Any())
                    {
                        potentialWaterVoters.Add(new
                        {
                            voter.LalVoterId,
                            FullName = $"{voter.FirstName} {voter.LastName}",
                            voter.AddressLine,
                            voter.City,
                            voter.State,
                            voter.Zip,
                            voter.Latitude,
                            voter.Longitude,
                            GoogleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={voter.Latitude},{voter.Longitude}",
                            StreetViewUrl = $"https://www.google.com/maps/@{voter.Latitude},{voter.Longitude},3a,75y,90t/data=!3m6!1e1!3m4!1s0x0:0x0!2e0!7i16384!8i8192",
                            PotentialWaterBodies = waterBodyMatches,
                            AddressIndicators = addressMatches,
                            FullAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}",
                            Warning = waterBodyMatches.Any() ? "Coordinates fall within known water body area" : "Address contains water-related keywords"
                        });
                    }
                }

                var result = new
                {
                    TotalChecked = votersWithCoords.Count,
                    PotentialWaterVoters = potentialWaterVoters.Count,
                    Voters = potentialWaterVoters.OrderByDescending(v => ((dynamic)v).PotentialWaterBodies.Count).ToList(),
                    WaterBodyRangesUsed = waterBodyRanges.Select(w => w.Name).ToList(),
                    Note = "Review Google Maps links to verify if addresses are actually in water. Some may be waterfront properties."
                };

                _logger.LogInformation("Water body geocoding check completed. Checked {Total} voters, found {Suspicious} potentially in water", 
                    votersWithCoords.Count, potentialWaterVoters.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking water body geocoding");
                return StatusCode(500, new { error = "Failed to check water body geocoding" });
            }
        }

        [HttpPut("{id}/coordinates")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateVoterCoordinates(string id, [FromBody] UpdateCoordinatesRequest request)
        {
            try
            {
                var voter = await _context.Voters.FindAsync(id);
                if (voter == null)
                {
                    return NotFound(new { error = "Voter not found" });
                }

                var oldLat = voter.Latitude;
                var oldLon = voter.Longitude;

                voter.Latitude = request.Latitude;
                voter.Longitude = request.Longitude;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated coordinates for voter {VoterId} from ({OldLat},{OldLon}) to ({NewLat},{NewLon}) by user {UserId}", 
                    id, oldLat, oldLon, request.Latitude, request.Longitude, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                return Ok(new 
                { 
                    message = "Coordinates updated successfully",
                    oldCoordinates = new { latitude = oldLat, longitude = oldLon },
                    newCoordinates = new { latitude = request.Latitude, longitude = request.Longitude }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating coordinates for voter {VoterId}", id);
                return StatusCode(500, new { error = "Failed to update coordinates" });
            }
        }

        [HttpPost("bulk-fix-coordinates")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> BulkFixCoordinates([FromBody] BulkFixCoordinatesRequest request)
        {
            try
            {
                var results = new List<object>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var fix in request.Fixes)
                {
                    try
                    {
                        var voter = await _context.Voters.FindAsync(fix.LalVoterId);
                        if (voter != null)
                        {
                            var oldLat = voter.Latitude;
                            var oldLon = voter.Longitude;

                            voter.Latitude = fix.Latitude;
                            voter.Longitude = fix.Longitude;

                            await _context.SaveChangesAsync();
                            successCount++;

                            results.Add(new
                            {
                                fix.LalVoterId,
                                Status = "Success",
                                OldCoordinates = new { latitude = oldLat, longitude = oldLon },
                                NewCoordinates = new { latitude = fix.Latitude, longitude = fix.Longitude }
                            });

                            _logger.LogInformation("Bulk update: Fixed coordinates for voter {VoterId}", fix.LalVoterId);
                        }
                        else
                        {
                            failureCount++;
                            results.Add(new
                            {
                                fix.LalVoterId,
                                Status = "Failed",
                                Error = "Voter not found"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        results.Add(new
                        {
                            fix.LalVoterId,
                            Status = "Failed",
                            Error = ex.Message
                        });
                        _logger.LogError(ex, "Error updating coordinates for voter {VoterId} in bulk operation", fix.LalVoterId);
                    }
                }

                return Ok(new
                {
                    TotalProcessed = request.Fixes.Count,
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk coordinate fix operation");
                return StatusCode(500, new { error = "Failed to process bulk coordinate fixes" });
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

    public class UpdateCoordinatesRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class BulkFixCoordinatesRequest
    {
        public List<CoordinateFix> Fixes { get; set; } = new List<CoordinateFix>();
    }

    public class CoordinateFix
    {
        public string LalVoterId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}