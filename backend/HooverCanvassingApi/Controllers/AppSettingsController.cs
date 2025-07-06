using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;

namespace HooverCanvassingApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppSettingsController> _logger;

    public AppSettingsController(ApplicationDbContext context, ILogger<AppSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("public")]
    public async Task<ActionResult<Dictionary<string, string>>> GetPublicSettings()
    {
        var settings = await _context.AppSettings
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return Ok(settings);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings()
    {
        var settings = await _context.AppSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return Ok(settings);
    }

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<AppSetting>>> GetAllSettings()
    {
        var settings = await _context.AppSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();

        return Ok(settings);
    }

    [HttpGet("{key}")]
    [Authorize]
    public async Task<ActionResult<string>> GetSetting(string key)
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            return NotFound();
        }

        return Ok(setting.Value);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AppSetting>> CreateSetting(AppSetting setting)
    {
        if (await _context.AppSettings.AnyAsync(s => s.Key == setting.Key))
        {
            return Conflict($"Setting with key '{setting.Key}' already exists");
        }

        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = User.Identity?.Name ?? "Unknown";

        _context.AppSettings.Add(setting);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new app setting: {Key} by {User}", setting.Key, User.Identity?.Name);

        return CreatedAtAction(nameof(GetSetting), new { key = setting.Key }, setting);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateSetting(int id, AppSetting setting)
    {
        if (id != setting.Id)
        {
            return BadRequest();
        }

        var existingSetting = await _context.AppSettings.FindAsync(id);
        if (existingSetting == null)
        {
            return NotFound();
        }

        existingSetting.Value = setting.Value;
        existingSetting.Description = setting.Description;
        existingSetting.Category = setting.Category;
        existingSetting.IsPublic = setting.IsPublic;
        existingSetting.UpdatedAt = DateTime.UtcNow;
        existingSetting.UpdatedBy = User.Identity?.Name ?? "Unknown";

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated app setting: {Key} by {User}", existingSetting.Key, User.Identity?.Name);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SettingExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteSetting(int id)
    {
        var setting = await _context.AppSettings.FindAsync(id);
        if (setting == null)
        {
            return NotFound();
        }

        _context.AppSettings.Remove(setting);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted app setting: {Key} by {User}", setting.Key, User.Identity?.Name);

        return NoContent();
    }

    [HttpPost("migrate-from-env")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> MigrateFromEnvironment()
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var migrated = new List<string>();

        var settingsToMigrate = new Dictionary<string, (string description, string category, bool isPublic)>
        {
            ["CAMPAIGN_WEBSITE"] = ("Campaign website URL", "Campaign", true),
            ["CAMPAIGN_YOUTUBE"] = ("Campaign YouTube channel URL", "Campaign", true),
            ["CAMPAIGN_VENMO"] = ("Campaign Venmo handle", "Campaign", true),
            ["VOTER_REGISTRATION_URL"] = ("Voter registration URL", "Voter Resources", true),
            ["VOLUNTEER_HOTLINE"] = ("Volunteer hotline phone number", "Support", true),
            ["SUPPORT_EMAIL"] = ("Support email address", "Support", true),
            ["CAMPAIGN_NAME"] = ("Campaign name", "Campaign", true),
            ["GOOGLE_MAPS_API_KEY"] = ("Google Maps API key", "API Keys", false),
            ["TWILIO_ACCOUNT_SID"] = ("Twilio Account SID", "API Keys", false),
            ["TWILIO_AUTH_TOKEN"] = ("Twilio Auth Token", "API Keys", false),
            ["TWILIO_FROM_PHONE"] = ("Twilio phone number", "API Keys", false),
            ["TWILIO_MESSAGING_SERVICE_SID"] = ("Twilio Messaging Service SID", "API Keys", false),
            ["EMAIL_FROM_ADDRESS"] = ("Email from address", "Email", false),
            ["EMAIL_FROM_NAME"] = ("Email from name", "Email", false)
        };

        foreach (var (key, (description, category, isPublic)) in settingsToMigrate)
        {
            var value = configuration[key];
            if (!string.IsNullOrEmpty(value) && !await _context.AppSettings.AnyAsync(s => s.Key == key))
            {
                var setting = new AppSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    Category = category,
                    IsPublic = isPublic,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = User.Identity?.Name ?? "System"
                };

                _context.AppSettings.Add(setting);
                migrated.Add(key);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Migrated {Count} settings from environment variables by {User}", migrated.Count, User.Identity?.Name);

        return Ok(new { migratedCount = migrated.Count, migratedKeys = migrated });
    }

    private bool SettingExists(int id)
    {
        return _context.AppSettings.Any(e => e.Id == id);
    }
}