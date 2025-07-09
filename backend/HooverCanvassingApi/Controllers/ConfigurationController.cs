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
            
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { error = "Google Maps API key not configured" });
            }
            
            return Ok(new { apiKey });
        }
    }
}