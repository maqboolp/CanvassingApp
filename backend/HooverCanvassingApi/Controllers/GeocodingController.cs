using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class GeocodingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeocodingController> _logger;

        public GeocodingController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeocodingController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("verify-water-locations")]
        public async Task<IActionResult> VerifyWaterLocations()
        {
            var waterBoundaries = new[]
            {
                new { Name = "Lake Purdy", MinLat = 33.430, MaxLat = 33.450, MinLng = -86.630, MaxLng = -86.600 },
                new { Name = "Cahaba River", MinLat = 33.380, MaxLat = 33.400, MinLng = -86.820, MaxLng = -86.800 }
            };

            var votersInWater = new List<object>();

            foreach (var boundary in waterBoundaries)
            {
                var voters = await _context.Voters
                    .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                    .Where(v => v.Latitude >= boundary.MinLat && v.Latitude <= boundary.MaxLat)
                    .Where(v => v.Longitude >= boundary.MinLng && v.Longitude <= boundary.MaxLng)
                    .Select(v => new
                    {
                        v.LalVoterId,
                        v.FirstName,
                        v.LastName,
                        v.AddressLine,
                        v.Latitude,
                        v.Longitude,
                        WaterBody = boundary.Name
                    })
                    .ToListAsync();

                votersInWater.AddRange(voters);
            }

            return Ok(new
            {
                totalVotersInWater = votersInWater.Count,
                voters = votersInWater,
                message = $"Found {votersInWater.Count} voters with coordinates in water bodies"
            });
        }

        [HttpPost("regeoccode-address")]
        public async Task<IActionResult> RegeocodeAddress([FromBody] RegeocodeRequest request)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("Google Maps API key not configured");
            }

            var voter = await _context.Voters
                .FirstOrDefaultAsync(v => v.LalVoterId == request.VoterId);

            if (voter == null)
            {
                return NotFound("Voter not found");
            }

            var httpClient = _httpClientFactory.CreateClient();
            
            // Build a more specific address
            var fullAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}";
            var encodedAddress = Uri.EscapeDataString(fullAddress);
            
            // Add bounds to prefer results in Hoover, AL area
            var bounds = "33.3,-87.0|33.5,-86.6"; // Hoover, AL bounding box
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&bounds={bounds}&key={apiKey}";

            try
            {
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var geocodeResult = JsonSerializer.Deserialize<GoogleGeocodeResponse>(content);

                if (geocodeResult?.Status == "OK" && geocodeResult.Results?.Count > 0)
                {
                    var result = geocodeResult.Results[0];
                    var location = result.Geometry.Location;
                    
                    // Check if result is ROOFTOP or RANGE_INTERPOLATED (more accurate)
                    var locationType = result.Geometry.LocationType;
                    var isAccurate = locationType == "ROOFTOP" || locationType == "RANGE_INTERPOLATED";

                    // Verify the result is not in water by checking against known water boundaries
                    var isInWater = IsLocationInWater(location.Lat, location.Lng);

                    if (!isInWater && isAccurate)
                    {
                        // Update voter coordinates
                        voter.Latitude = location.Lat;
                        voter.Longitude = location.Lng;
                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            success = true,
                            voterId = voter.LalVoterId,
                            address = voter.AddressLine,
                            oldCoordinates = new { lat = request.OldLatitude, lng = request.OldLongitude },
                            newCoordinates = new { lat = location.Lat, lng = location.Lng },
                            locationType = locationType,
                            formattedAddress = result.FormattedAddress
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = false,
                            reason = isInWater ? "New location is still in water" : "Location accuracy too low",
                            locationType = locationType,
                            coordinates = new { lat = location.Lat, lng = location.Lng }
                        });
                    }
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        reason = "Geocoding failed",
                        status = geocodeResult?.Status,
                        message = geocodeResult?.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regeocoding address for voter {VoterId}", request.VoterId);
                return StatusCode(500, new { error = "Failed to regeoccode address", details = ex.Message });
            }
        }

        [HttpPost("bulk-fix-water-locations")]
        public async Task<IActionResult> BulkFixWaterLocations()
        {
            var votersInWater = await GetVotersInWater();
            var results = new List<object>();
            var apiKey = _configuration["GoogleMaps:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest("Google Maps API key not configured");
            }

            foreach (var voter in votersInWater)
            {
                // Add delay to respect Google's rate limits
                await Task.Delay(100);

                var result = await RegeocodeVoter(voter, apiKey);
                results.Add(result);
            }

            var summary = new
            {
                totalProcessed = results.Count,
                successful = results.Count(r => ((dynamic)r).success),
                failed = results.Count(r => !((dynamic)r).success),
                results = results
            };

            return Ok(summary);
        }

        private bool IsLocationInWater(double lat, double lng)
        {
            // Known water body boundaries in Hoover area
            var waterBodies = new[]
            {
                new { MinLat = 33.430, MaxLat = 33.450, MinLng = -86.630, MaxLng = -86.600 }, // Lake Purdy
                new { MinLat = 33.380, MaxLat = 33.400, MinLng = -86.820, MaxLng = -86.800 }, // Cahaba River
            };

            return waterBodies.Any(w => 
                lat >= w.MinLat && lat <= w.MaxLat && 
                lng >= w.MinLng && lng <= w.MaxLng);
        }

        private async Task<List<Voter>> GetVotersInWater()
        {
            return await _context.Voters
                .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                .Where(v => 
                    // Lake Purdy area
                    (v.Latitude >= 33.430 && v.Latitude <= 33.450 && v.Longitude >= -86.630 && v.Longitude <= -86.600) ||
                    // Cahaba River areas
                    (v.Latitude >= 33.380 && v.Latitude <= 33.400 && v.Longitude >= -86.820 && v.Longitude <= -86.800)
                )
                .ToListAsync();
        }

        private async Task<object> RegeocodeVoter(Voter voter, string apiKey)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var fullAddress = $"{voter.AddressLine}, {voter.City}, {voter.State} {voter.Zip}";
            var encodedAddress = Uri.EscapeDataString(fullAddress);
            var bounds = "33.3,-87.0|33.5,-86.6";
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&bounds={bounds}&key={apiKey}";

            try
            {
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var geocodeResult = JsonSerializer.Deserialize<GoogleGeocodeResponse>(content);

                if (geocodeResult?.Status == "OK" && geocodeResult.Results?.Count > 0)
                {
                    var result = geocodeResult.Results[0];
                    var location = result.Geometry.Location;
                    var locationType = result.Geometry.LocationType;
                    var isAccurate = locationType == "ROOFTOP" || locationType == "RANGE_INTERPOLATED";
                    var isInWater = IsLocationInWater(location.Lat, location.Lng);

                    if (!isInWater && isAccurate)
                    {
                        var oldLat = voter.Latitude;
                        var oldLng = voter.Longitude;
                        
                        voter.Latitude = location.Lat;
                        voter.Longitude = location.Lng;
                        await _context.SaveChangesAsync();

                        return new
                        {
                            success = true,
                            voterId = voter.LalVoterId,
                            address = voter.AddressLine,
                            oldCoordinates = new { lat = oldLat, lng = oldLng },
                            newCoordinates = new { lat = location.Lat, lng = location.Lng },
                            locationType = locationType
                        };
                    }
                }

                return new
                {
                    success = false,
                    voterId = voter.LalVoterId,
                    address = voter.AddressLine,
                    reason = "Could not find accurate non-water location"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    voterId = voter.LalVoterId,
                    address = voter.AddressLine,
                    error = ex.Message
                };
            }
        }
    }

    public class RegeocodeRequest
    {
        public string VoterId { get; set; } = string.Empty;
        public double? OldLatitude { get; set; }
        public double? OldLongitude { get; set; }
    }

    public class GoogleGeocodeResponse
    {
        [JsonPropertyName("results")]
        public List<GoogleGeocodeResult> Results { get; set; } = new();
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    public class GoogleGeocodeResult
    {
        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; } = string.Empty;
        
        [JsonPropertyName("geometry")]
        public GoogleGeometry Geometry { get; set; } = new();
    }

    public class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleLocation Location { get; set; } = new();
        
        [JsonPropertyName("location_type")]
        public string LocationType { get; set; } = string.Empty;
    }

    public class GoogleLocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}