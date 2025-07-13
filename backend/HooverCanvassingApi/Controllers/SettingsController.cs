using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using HooverCanvassingApi.Configuration;
using System.Text.Json;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;

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
        private readonly ApplicationDbContext _dbContext;

        public SettingsController(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<SettingsController> logger,
            ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _appsettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            _dbContext = dbContext;
        }

        [HttpGet("twilio")]
        public async Task<ActionResult<TwilioSettings>> GetTwilioSettings()
        {
            // Try to get settings from database first
            var dbSettings = await _dbContext.TwilioConfigurations
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (dbSettings != null)
            {
                return Ok(new TwilioSettings
                {
                    AccountSid = dbSettings.AccountSid,
                    FromPhoneNumber = dbSettings.FromPhoneNumber ?? "",
                    SmsPhoneNumber = dbSettings.SmsPhoneNumber ?? "",
                    MessagingServiceSid = dbSettings.MessagingServiceSid ?? "",
                    HasAuthToken = !string.IsNullOrEmpty(dbSettings.AuthToken)
                });
            }

            // Fall back to configuration file
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
                // Get existing settings from database or create new
                var dbSettings = await _dbContext.TwilioConfigurations
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (dbSettings == null)
                {
                    // Create new settings if none exist
                    dbSettings = new TwilioConfiguration
                    {
                        AccountSid = "",
                        AuthToken = "",
                        IsActive = true
                    };
                    _dbContext.TwilioConfigurations.Add(dbSettings);
                }

                // Update only provided values
                if (!string.IsNullOrEmpty(request.AccountSid))
                    dbSettings.AccountSid = request.AccountSid;

                if (!string.IsNullOrEmpty(request.AuthToken))
                    dbSettings.AuthToken = request.AuthToken;

                if (request.FromPhoneNumber != null)
                    dbSettings.FromPhoneNumber = request.FromPhoneNumber;

                if (request.SmsPhoneNumber != null)
                    dbSettings.SmsPhoneNumber = request.SmsPhoneNumber;

                if (request.MessagingServiceSid != null)
                    dbSettings.MessagingServiceSid = request.MessagingServiceSid;

                dbSettings.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Twilio settings updated successfully in database");

                return Ok(new { message = "Twilio settings updated successfully." });
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