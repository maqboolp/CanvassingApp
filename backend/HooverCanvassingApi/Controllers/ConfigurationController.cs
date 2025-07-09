using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        
        public ConfigurationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        [HttpGet("google-maps-key")]
        public IActionResult GetGoogleMapsKey()
        {
            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_GEOCODING_API_KEY");
            
            // Also check configuration as fallback
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = _configuration["GoogleGeocodingApiKey"];
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                // Log for debugging
                Console.WriteLine("GOOGLE_GEOCODING_API_KEY environment variable not found");
                Console.WriteLine($"Current environment variables: {Environment.GetEnvironmentVariables().Count} total");
                
                return BadRequest(new { error = "Google Maps API key not configured" });
            }
            
            return Ok(new { apiKey });
        }
    }
}