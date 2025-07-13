using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using HooverCanvassingApi.Configuration;
using System.Text.Json;

namespace HooverCanvassingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SettingsController> _logger;
        private readonly string _appsettingsPath;

        public SettingsController(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<SettingsController> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _appsettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        }

        [HttpGet("twilio")]
        public ActionResult<TwilioSettings> GetTwilioSettings()
        {
            var settings = new TwilioSettings
            {
                AccountSid = _configuration["Twilio:AccountSid"] ?? "",
                FromPhoneNumber = _configuration["Twilio:FromPhoneNumber"] ?? "",
                SmsPhoneNumber = _configuration["Twilio:SmsPhoneNumber"] ?? "",
                MessagingServiceSid = _configuration["Twilio:MessagingServiceSid"] ?? "",
                // Don't expose auth token in GET requests
                HasAuthToken = !string.IsNullOrEmpty(_configuration["Twilio:AuthToken"])
            };

            return Ok(settings);
        }

        [HttpPost("twilio")]
        public async Task<ActionResult> UpdateTwilioSettings([FromBody] UpdateTwilioSettingsRequest request)
        {
            try
            {
                // Read existing appsettings.json
                var json = await System.IO.File.ReadAllTextAsync(_appsettingsPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    return StatusCode(500, new { message = "Failed to read configuration" });
                }

                // Get or create Twilio section
                if (!config.ContainsKey("Twilio"))
                {
                    config["Twilio"] = new Dictionary<string, object>();
                }

                var twilioSection = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(config["Twilio"]),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new Dictionary<string, object>();

                // Update only provided values
                if (!string.IsNullOrEmpty(request.AccountSid))
                    twilioSection["AccountSid"] = request.AccountSid;

                if (!string.IsNullOrEmpty(request.AuthToken))
                    twilioSection["AuthToken"] = request.AuthToken;

                if (request.FromPhoneNumber != null)
                    twilioSection["FromPhoneNumber"] = request.FromPhoneNumber;

                if (request.SmsPhoneNumber != null)
                    twilioSection["SmsPhoneNumber"] = request.SmsPhoneNumber;

                if (request.MessagingServiceSid != null)
                    twilioSection["MessagingServiceSid"] = request.MessagingServiceSid;

                config["Twilio"] = twilioSection;

                // Write back to appsettings.json
                var updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await System.IO.File.WriteAllTextAsync(_appsettingsPath, updatedJson);

                _logger.LogInformation("Twilio settings updated successfully");

                return Ok(new { message = "Twilio settings updated successfully. Please restart the application for changes to take effect." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Twilio settings");
                return StatusCode(500, new { message = "Failed to update settings" });
            }
        }
    }

    public class TwilioSettings
    {
        public string AccountSid { get; set; } = "";
        public string FromPhoneNumber { get; set; } = "";
        public string SmsPhoneNumber { get; set; } = "";
        public string MessagingServiceSid { get; set; } = "";
        public bool HasAuthToken { get; set; }
    }

    public class UpdateTwilioSettingsRequest
    {
        public string? AccountSid { get; set; }
        public string? AuthToken { get; set; }
        public string? FromPhoneNumber { get; set; }
        public string? SmsPhoneNumber { get; set; }
        public string? MessagingServiceSid { get; set; }
    }
}