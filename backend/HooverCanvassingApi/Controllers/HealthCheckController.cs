using Microsoft.AspNetCore.Mvc;

namespace HooverCanvassingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public ActionResult GetHealth()
        {
            return Ok(new 
            { 
                status = "healthy",
                timestamp = DateTime.UtcNow,
                controllers = new[]
                {
                    "SettingsController",
                    "PhoneNumberPoolController",
                    "HealthCheckController"
                }
            });
        }
        
        [HttpGet("settings")]
        public ActionResult CheckSettings()
        {
            var hasSettingsController = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Any(t => t.Name == "SettingsController");
                
            var hasPhoneNumberPoolController = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Any(t => t.Name == "PhoneNumberPoolController");
                
            return Ok(new 
            { 
                hasSettingsController,
                hasPhoneNumberPoolController
            });
        }
    }
}