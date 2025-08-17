using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Middleware;
using HooverCanvassingApi.Services;
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
        private readonly IGoogleMapsService _googleMapsService;

        public VotersController(ApplicationDbContext context, ILogger<VotersController> logger, IGoogleMapsService googleMapsService)
        {
            _context = context;
            _logger = logger;
            _googleMapsService = googleMapsService;
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
            [FromQuery] double radiusKm = 3.2, // Default 2 miles
            [FromQuery] string? partyAffiliation = null,
            [FromQuery] List<int>? tagIds = null,
            [FromQuery] bool hasPhoneNumber = false,
            [FromQuery] bool useTravelDistance = false,
            [FromQuery] string travelMode = "driving")
        {
            try
            {
                _logger.LogInformation("GetVoters called with parameters: page={Page}, limit={Limit}, contactStatus={ContactStatus}, sortBy={SortBy}, sortOrder={SortOrder}, zipCode={ZipCode}, searchName={SearchName}, radiusKm={RadiusKm}, useTravelDistance={UseTravelDistance}, travelMode={TravelMode}", 
                    page, limit, contactStatus, sortBy, sortOrder, zipCode, searchName, radiusKm, useTravelDistance, travelMode);
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
                    _logger.LogInformation("Applying contact status filter: {ContactStatus}", contactStatus);
                    switch (contactStatus.ToLower())
                    {
                        case "contacted":
                            query = query.Where(v => v.IsContacted);
                            break;
                        case "not-contacted":
                            query = query.Where(v => !v.IsContacted);
                            break;
                        case "all":
                            // No filter needed for "all"
                            _logger.LogInformation("Contact status 'all' - no filter applied");
                            break;
                        default:
                            _logger.LogWarning("Unknown contact status: {ContactStatus}", contactStatus);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(partyAffiliation))
                {
                    query = query.Where(v => v.PartyAffiliation == partyAffiliation);
                }

                // Filter by phone number
                if (hasPhoneNumber)
                {
                    query = query.Where(v => !string.IsNullOrEmpty(v.CellPhone));
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

                _logger.LogInformation("Executing query to fetch voters...");
                
                var voters = await query
                    .Include(v => v.TagAssignments)
                        .ThenInclude(ta => ta.Tag)
                    .ToListAsync();
                
                _logger.LogInformation("Query executed successfully. Found {VoterCount} voters", voters.Count);

                // Apply distance filtering and sorting in memory if location is provided
                List<(Voter Voter, double Distance, bool IsStraightLine)> votersWithDistance = null;
                if (latitude.HasValue && longitude.HasValue)
                {
                    if (useTravelDistance)
                    {
                        // Use Google Maps for travel distance
                        _logger.LogInformation("Using Google Maps API for travel distance calculation");
                        
                        // First, pre-filter using straight-line distance with a buffer
                        // Travel distance can be 1.3-2x straight-line distance depending on road layout
                        // For very short distances, use a larger multiplier as roads can add significant detours
                        var preFilterRadiusKm = radiusKm < 5 ? radiusKm * 5.0 : radiusKm * 2.0;
                        
                        _logger.LogInformation("Pre-filtering voters with radius {RadiusKm}km, using buffer of {PreFilterRadiusKm}km", 
                            radiusKm, preFilterRadiusKm);
                        var preFilteredVoters = voters
                            .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                            .Select(v => new {
                                Voter = v,
                                StraightLineDistance = CalculateDistance(latitude.Value, longitude.Value, v.Latitude!.Value, v.Longitude!.Value)
                            })
                            .Where(v => v.StraightLineDistance <= preFilterRadiusKm)
                            .OrderBy(v => v.StraightLineDistance)
                            .Take(150) // Limit to 150 nearest voters to reduce API calls (increased to ensure we find walkable routes)
                            .ToList();
                        
                        _logger.LogInformation("Pre-filtered from {TotalCount} to {FilteredCount} voters using straight-line distance", 
                            voters.Count(v => v.Latitude.HasValue && v.Longitude.HasValue), 
                            preFilteredVoters.Count);
                        
                        // Log the user's location for debugging
                        _logger.LogInformation("User location for distance calculation: {Lat}, {Lng} (Mode: {Mode})", 
                            latitude.Value.ToString("F6"), longitude.Value.ToString("F6"), travelMode);
                        
                        // Log a few sample destinations for debugging
                        if (preFilteredVoters.Count > 0)
                        {
                            var sampleVoters = preFilteredVoters.Take(3);
                            foreach (var sample in sampleVoters)
                            {
                                _logger.LogInformation("Sample destination: {Name} at {Lat}, {Lng} (straight-line: {Distance}km)", 
                                    $"{sample.Voter.FirstName} {sample.Voter.LastName}",
                                    sample.Voter.Latitude!.Value.ToString("F6"), 
                                    sample.Voter.Longitude!.Value.ToString("F6"),
                                    sample.StraightLineDistance.ToString("F2"));
                            }
                        }
                        
                        if (preFilteredVoters.Any())
                        {
                            // Get travel distances in batches for pre-filtered voters
                            var destinations = preFilteredVoters.Select(v => (v.Voter.Latitude!.Value, v.Voter.Longitude!.Value)).ToList();
                            var distanceResults = await _googleMapsService.GetBatchTravelDistancesAsync(
                                latitude.Value, 
                                longitude.Value, 
                                destinations,
                                travelMode
                            );
                            
                            votersWithDistance = new List<(Voter, double, bool)>();
                            int withinRange = 0;
                            int outOfRange = 0;
                            
                            for (int i = 0; i < preFilteredVoters.Count && i < distanceResults.Count; i++)
                            {
                                var distResult = distanceResults[i];
                                if (distResult != null)
                                {
                                    // Check if the travel distance is unrealistic compared to straight-line
                                    // Walking should rarely be more than 2x straight-line distance in urban areas
                                    var straightLineDist = preFilteredVoters[i].StraightLineDistance;
                                    var travelDistRatio = distResult.DistanceInKm / straightLineDist;
                                    
                                    // Check for unrealistic walking distances or distances over 2 miles
                                    var maxWalkingDistanceKm = 3.2; // 2 miles
                                    if ((travelDistRatio > 5.0 && travelMode == "walking") || 
                                        (travelMode == "walking" && distResult.DistanceInKm > maxWalkingDistanceKm))
                                    {
                                        if (distResult.DistanceInKm > maxWalkingDistanceKm)
                                        {
                                            _logger.LogWarning("Walking distance exceeds 2 mile limit for voter {VoterId}: travel {TravelKm}km > {MaxKm}km", 
                                                preFilteredVoters[i].Voter.LalVoterId, 
                                                distResult.DistanceInKm.ToString("F2"), 
                                                maxWalkingDistanceKm);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Unrealistic walking distance detected for voter {VoterId}: travel {TravelKm}km is {Ratio}x straight-line {StraightKm}km", 
                                                preFilteredVoters[i].Voter.LalVoterId, 
                                                distResult.DistanceInKm.ToString("F2"), 
                                                travelDistRatio.ToString("F1"),
                                                straightLineDist.ToString("F2"));
                                        }
                                        
                                        // Use straight-line distance as fallback for unrealistic travel distances
                                        if (straightLineDist <= radiusKm)
                                        {
                                            votersWithDistance.Add((preFilteredVoters[i].Voter, straightLineDist, true));
                                            withinRange++;
                                        }
                                    }
                                    else if (distResult.DistanceInKm <= radiusKm)
                                    {
                                        votersWithDistance.Add((preFilteredVoters[i].Voter, distResult.DistanceInKm, false));
                                        withinRange++;
                                    }
                                    else
                                    {
                                        outOfRange++;
                                        _logger.LogDebug("Voter {VoterId} excluded: travel distance {TravelKm}km > {RadiusKm}km (straight-line was {StraightKm}km)", 
                                            preFilteredVoters[i].Voter.LalVoterId, 
                                            distResult.DistanceInKm.ToString("F2"), 
                                            radiusKm,
                                            preFilteredVoters[i].StraightLineDistance.ToString("F2"));
                                    }
                                }
                            }
                            
                            _logger.LogInformation("Travel distance results: {WithinRange} within {RadiusKm}km, {OutOfRange} beyond range", 
                                withinRange, radiusKm, outOfRange);
                            
                            // If travel distance returned very few results, fall back to straight-line distance
                            if (votersWithDistance.Count < 5 && preFilteredVoters.Count > votersWithDistance.Count)
                            {
                                _logger.LogWarning("Travel distance returned only {TravelCount} voters out of {TotalCount} candidates. Falling back to straight-line distance for better coverage.", 
                                    votersWithDistance.Count, preFilteredVoters.Count);
                                
                                // Include voters where travel distance failed but straight-line distance is reasonable
                                foreach (var candidate in preFilteredVoters.Where(p => p.StraightLineDistance <= radiusKm))
                                {
                                    if (!votersWithDistance.Any(vd => vd.Voter.LalVoterId == candidate.Voter.LalVoterId))
                                    {
                                        votersWithDistance.Add((candidate.Voter, candidate.StraightLineDistance, true));
                                    }
                                }
                            }
                            
                            votersWithDistance = votersWithDistance.OrderBy(vd => vd.Distance).ToList();
                            voters = votersWithDistance.Select(vd => vd.Voter).ToList();
                            
                            _logger.LogInformation("Final count after travel distance filtering (with fallback): {Count} voters", voters.Count);
                        }
                        else
                        {
                            voters = new List<Voter>();
                        }
                    }
                    else
                    {
                        // Use straight-line distance (existing code)
                        votersWithDistance = voters
                            .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                            .Select(v => (
                                Voter: v,
                                Distance: CalculateDistance(latitude.Value, longitude.Value, v.Latitude!.Value, v.Longitude!.Value),
                                IsStraightLine: true
                            ))
                            .Where(vd => vd.Distance <= radiusKm)
                            .OrderBy(vd => vd.Distance) // Sort by distance from closest to farthest
                            .ToList();
                        
                        voters = votersWithDistance.Select(vd => vd.Voter).ToList();
                    }
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
                        bool? isStraightLine = null;
                        if (votersWithDistance != null && latitude.HasValue && longitude.HasValue)
                        {
                            var voterWithDist = votersWithDistance.FirstOrDefault(vd => vd.Voter.LalVoterId == v.LalVoterId);
                            if (voterWithDist.Voter != null)
                            {
                                distance = voterWithDist.Distance;
                                isStraightLine = voterWithDist.IsStraightLine;
                            }
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
                            DistanceKm = distance,
                            DistanceIsStraightLine = isStraightLine
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
                _logger.LogError(ex, "Error retrieving voters with parameters: page={Page}, limit={Limit}, contactStatus={ContactStatus}, sortBy={SortBy}, sortOrder={SortOrder}. Error: {ErrorMessage}", 
                    page, limit, contactStatus, sortBy, sortOrder, ex.Message);
                return StatusCode(500, new { error = "Failed to retrieve voters", details = ex.Message });
            }
        }

        [HttpPost("{voterId}/unlock")]
        public async Task<IActionResult> UnlockVoter(string voterId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var voterLock = await _context.VoterLocks
                .Where(l => l.VoterId == voterId && l.UserId == currentUserId && l.IsActive)
                .FirstOrDefaultAsync();

            if (voterLock != null)
            {
                voterLock.IsActive = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Unlocked voter {VoterId} for user {UserId}", voterId, currentUserId);
            }

            return Ok(new { message = "Voter unlocked" });
        }

        [HttpGet("locked-voters")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<object>>> GetLockedVoters()
        {
            var lockedVoters = await _context.VoterLocks
                .Where(l => l.IsActive && l.ExpiresAt > DateTime.UtcNow)
                .Include(l => l.Voter)
                .Select(l => new
                {
                    voterId = l.VoterId,
                    voterName = l.Voter != null ? $"{l.Voter.FirstName} {l.Voter.LastName}" : "Unknown",
                    lockedBy = l.UserName,
                    lockedAt = l.LockedAt,
                    expiresAt = l.ExpiresAt
                })
                .ToListAsync();

            return Ok(lockedVoters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VoterDto>> GetVoter(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedException("User not authenticated");
            }

            var voter = await _context.Voters
                .Include(v => v.Contacts.OrderByDescending(c => c.Timestamp))
                .Include(v => v.TagAssignments)
                    .ThenInclude(ta => ta.Tag)
                .FirstOrDefaultAsync(v => v.LalVoterId == id);

            if (voter == null)
            {
                throw new NotFoundException("Voter", id);
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

        [HttpGet("next-to-call")]
        public async Task<ActionResult<VoterDto>> GetNextVoterToCall([FromQuery] string? zip = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _context.Volunteers.FindAsync(currentUserId);
            _logger.LogInformation("Getting next voter to call for user {UserId}, ZIP: {Zip}", currentUserId, zip);

            try
            {
                // First, release any expired locks
                var expiredLocks = await _context.VoterLocks
                    .Where(l => l.IsActive && l.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();
                
                foreach (var expiredLock in expiredLocks)
                {
                    expiredLock.IsActive = false;
                }
                
                // Get currently locked voter IDs
                var lockedVoterIds = await _context.VoterLocks
                    .Where(l => l.IsActive && l.ExpiresAt > DateTime.UtcNow)
                    .Select(l => l.VoterId)
                    .ToListAsync();

                // Get voters with phone numbers who haven't been called yet and aren't locked
                var query = _context.Voters
                    .Where(v => !string.IsNullOrEmpty(v.CellPhone))
                    .Where(v => !v.IsContacted || v.LastContactStatus == ContactStatus.NotHome)
                    .Where(v => !lockedVoterIds.Contains(v.LalVoterId));

                // Filter by ZIP if provided
                if (!string.IsNullOrEmpty(zip))
                {
                    query = query.Where(v => v.Zip == zip);
                }

                // Order by those who have never been contacted first, then by age (older first)
                var voter = await query
                    .OrderBy(v => v.IsContacted ? 1 : 0)
                    .ThenByDescending(v => v.Age)
                    .FirstOrDefaultAsync();

                if (voter == null)
                {
                    _logger.LogInformation("No voters available to call");
                    return NotFound(new { message = "No voters available to call" });
                }

                // Create a lock for this voter
                var voterLock = new VoterLock
                {
                    VoterId = voter.LalVoterId,
                    UserId = currentUserId ?? "",
                    UserName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "Unknown",
                    LockedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    IsActive = true
                };

                _context.VoterLocks.Add(voterLock);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Locked voter {VoterId} for user {UserId}", voter.LalVoterId, currentUserId);

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
                    VoterSupport = voter.VoterSupport?.ToString()
                };

                return Ok(voterDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next voter to call");
                return StatusCode(500, "An error occurred while fetching the next voter");
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
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
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

        [HttpGet("test-distance")]
        public async Task<ActionResult> TestDistance(
            [FromQuery] double fromLat,
            [FromQuery] double fromLng,
            [FromQuery] double toLat,
            [FromQuery] double toLng,
            [FromQuery] string mode = "walking")
        {
            try
            {
                _logger.LogInformation("Testing distance from {FromLat},{FromLng} to {ToLat},{ToLng} via {Mode}", 
                    fromLat, fromLng, toLat, toLng, mode);
                
                // Calculate straight-line distance
                var straightLineKm = CalculateDistance(fromLat, fromLng, toLat, toLng);
                
                // Get travel distance
                var travelResult = await _googleMapsService.GetTravelDistanceAsync(fromLat, fromLng, toLat, toLng, mode);
                
                return Ok(new 
                {
                    straightLineDistance = new 
                    {
                        km = straightLineKm,
                        miles = straightLineKm * 0.621371
                    },
                    travelDistance = travelResult != null ? new 
                    {
                        km = travelResult.DistanceInKm,
                        miles = travelResult.DistanceInMiles,
                        text = travelResult.DistanceText,
                        duration = travelResult.DurationText
                    } : null,
                    coordinates = new 
                    {
                        from = $"{fromLat},{fromLng}",
                        to = $"{toLat},{toLng}"
                    },
                    mode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing distance");
                return StatusCode(500, new { error = "Failed to test distance", details = ex.Message });
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
        public bool? DistanceIsStraightLine { get; set; } // Indicates if distance is straight-line fallback
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
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}