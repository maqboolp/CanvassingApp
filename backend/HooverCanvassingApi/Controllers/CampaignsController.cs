using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;
using HooverCanvassingApi.Configuration;

namespace HooverCanvassingApi.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly ILogger<CampaignsController> _logger;
        private readonly CallingHoursSettings _callingHoursSettings;

        public CampaignsController(ICampaignService campaignService, ILogger<CampaignsController> logger, IConfiguration configuration)
        {
            _campaignService = campaignService;
            _logger = logger;
            _callingHoursSettings = configuration.GetSection("CallingHours").Get<CallingHoursSettings>() ?? new CallingHoursSettings();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetCampaigns()
        {
            var campaigns = await _campaignService.GetCampaignsAsync();
            return Ok(campaigns);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Campaign>> GetCampaign(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
                return NotFound();

            return Ok(campaign);
        }

        [HttpPost]
        public async Task<ActionResult<Campaign>> CreateCampaign(CreateCampaignRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var campaign = new Campaign
            {
                Name = request.Name,
                Message = request.Message ?? string.Empty,
                Type = request.Type,
                CreatedById = userId,
                VoiceUrl = request.VoiceUrl,
                VoiceRecordingId = request.VoiceRecordingId,
                // Email campaign fields
                EmailSubject = request.EmailSubject,
                EmailHtmlContent = request.EmailHtmlContent,
                EmailPlainTextContent = request.EmailPlainTextContent,
                FilterZipCodes = request.FilterZipCodes,
                FilterVoteFrequency = request.FilterVoteFrequency,
                FilterMinAge = request.FilterMinAge,
                FilterMaxAge = request.FilterMaxAge,
                FilterVoterSupport = request.FilterVoterSupport,
                FilterTags = request.FilterTagIds != null && request.FilterTagIds.Any() 
                    ? System.Text.Json.JsonSerializer.Serialize(request.FilterTagIds) 
                    : null,
                // Copy calling hours settings
                EnforceCallingHours = request.EnforceCallingHours,
                StartHour = request.StartHour,
                EndHour = request.EndHour,
                IncludeWeekends = request.IncludeWeekends,
                
                // Duplicate message prevention
                PreventDuplicateMessages = request.PreventDuplicateMessages
            };

            // Auto-generate voice URL for RoboCall campaigns
            if (campaign.Type == CampaignType.RoboCall)
            {
                if (campaign.VoiceRecordingId.HasValue)
                {
                    // Use voice recording
                    var voiceRecording = await _campaignService.GetVoiceRecordingAsync(campaign.VoiceRecordingId.Value);
                    if (voiceRecording != null)
                    {
                        var baseUrl = $"{Request.Scheme}://{Request.Host}";
                        campaign.VoiceUrl = $"{baseUrl}/api/TwilioWebhook/voice?audioUrl={Uri.EscapeDataString(voiceRecording.Url)}";
                        _logger.LogInformation($"Using voice recording for campaign: {voiceRecording.Name}");
                    }
                }
                else if (string.IsNullOrEmpty(campaign.VoiceUrl) && !string.IsNullOrEmpty(campaign.Message))
                {
                    // Use text-to-speech only if message is provided
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var encodedMessage = Uri.EscapeDataString(campaign.Message);
                    campaign.VoiceUrl = $"{baseUrl}/api/TwilioWebhook/voice?message={encodedMessage}";
                    _logger.LogInformation($"Auto-generated voice URL for campaign: {campaign.VoiceUrl}");
                }
            }

            var createdCampaign = await _campaignService.CreateCampaignAsync(campaign);
            return CreatedAtAction(nameof(GetCampaign), new { id = createdCampaign.Id }, createdCampaign);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Campaign>> UpdateCampaign(int id, UpdateCampaignRequest request)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
                return NotFound();

            if (campaign.Status != CampaignStatus.Draft)
                return BadRequest("Can only update campaigns that are ready to send");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Check ownership: SuperAdmins can edit any campaign, Admins can only edit their own
            if (userRole != "SuperAdmin" && campaign.CreatedById != userId)
                return Forbid("You can only edit campaigns you created");

            campaign.Name = request.Name;
            campaign.Message = request.Message ?? string.Empty;
            campaign.VoiceUrl = request.VoiceUrl;
            campaign.VoiceRecordingId = request.VoiceRecordingId;
            
            // Update email campaign fields
            campaign.EmailSubject = request.EmailSubject;
            campaign.EmailHtmlContent = request.EmailHtmlContent;
            campaign.EmailPlainTextContent = request.EmailPlainTextContent;
            
            campaign.FilterZipCodes = request.FilterZipCodes;
            campaign.FilterVoteFrequency = request.FilterVoteFrequency;
            campaign.FilterMinAge = request.FilterMinAge;
            campaign.FilterMaxAge = request.FilterMaxAge;
            campaign.FilterVoterSupport = request.FilterVoterSupport;
            
            // Update tag filters - Convert list to JSON string
            if (request.FilterTagIds != null && request.FilterTagIds.Count > 0)
            {
                campaign.FilterTags = System.Text.Json.JsonSerializer.Serialize(request.FilterTagIds);
            }
            else
            {
                campaign.FilterTags = null; // Clear tags if empty list is provided
            }
            
            // Update calling hours settings
            campaign.EnforceCallingHours = request.EnforceCallingHours;
            campaign.StartHour = request.StartHour;
            campaign.EndHour = request.EndHour;
            campaign.IncludeWeekends = request.IncludeWeekends;
            
            // Update duplicate message prevention
            campaign.PreventDuplicateMessages = request.PreventDuplicateMessages;

            // Auto-generate voice URL for RoboCall campaigns
            if (campaign.Type == CampaignType.RoboCall)
            {
                if (campaign.VoiceRecordingId.HasValue)
                {
                    // Use voice recording
                    var voiceRecording = await _campaignService.GetVoiceRecordingAsync(campaign.VoiceRecordingId.Value);
                    if (voiceRecording != null)
                    {
                        var baseUrl = $"{Request.Scheme}://{Request.Host}";
                        campaign.VoiceUrl = $"{baseUrl}/api/TwilioWebhook/voice?audioUrl={Uri.EscapeDataString(voiceRecording.Url)}";
                        _logger.LogInformation($"Using voice recording for campaign update: {voiceRecording.Name}");
                    }
                }
                else if (string.IsNullOrEmpty(campaign.VoiceUrl) && !string.IsNullOrEmpty(campaign.Message))
                {
                    // Use text-to-speech only if message is provided
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var encodedMessage = Uri.EscapeDataString(campaign.Message);
                    campaign.VoiceUrl = $"{baseUrl}/api/TwilioWebhook/voice?message={encodedMessage}";
                    _logger.LogInformation($"Auto-generated voice URL for campaign update: {campaign.VoiceUrl}");
                }
            }

            var updatedCampaign = await _campaignService.UpdateCampaignAsync(campaign);
            return Ok(updatedCampaign);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCampaign(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Check ownership: SuperAdmins can delete any campaign, Admins can only delete their own
            if (userRole != "SuperAdmin" && campaign.CreatedById != userId)
                return Forbid("You can only delete campaigns you created");

            // If campaign is sending and user is SuperAdmin, force stop it first
            if (campaign.Status == CampaignStatus.Sending && userRole == "SuperAdmin")
            {
                _logger.LogWarning($"SuperAdmin force stopping campaign {id} before deletion");
                await _campaignService.ForceStopCampaignAsync(id);
            }

            var success = await _campaignService.DeleteCampaignAsync(id);
            if (!success)
                return BadRequest("Campaign cannot be deleted. Only SuperAdmins can delete campaigns that are currently sending.");

            return NoContent();
        }

        [HttpGet("available-zipcodes")]
        public async Task<ActionResult> GetAvailableZipCodes()
        {
            try
            {
                var zipCodes = await _campaignService.GetAvailableZipCodesAsync();
                return Ok(zipCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("preview-audience")]
        public async Task<ActionResult> PreviewAudience(PreviewAudienceRequest request)
        {
            try
            {
                var count = await _campaignService.PreviewAudienceCountAsync(request.FilterZipCodes);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/send")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> SendCampaign(int id, SendCampaignRequest request)
        {
            try
            {
                var campaign = await _campaignService.GetCampaignAsync(id);
                if (campaign == null)
                    return NotFound(new { error = "Campaign not found" });

                // Check if it's a robocall campaign with enforced calling hours
                if (campaign.Type == CampaignType.RoboCall && campaign.EnforceCallingHours)
                {
                    try
                    {
                        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_callingHoursSettings.TimeZone);
                        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                        
                        // Check weekend restriction
                        if (!campaign.IncludeWeekends && (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday))
                        {
                            return BadRequest(new { error = $"Campaign cannot be sent on weekends. Calling hours are configured for weekdays only." });
                        }
                        
                        // Check hour restriction
                        if (localTime.Hour < campaign.StartHour || localTime.Hour >= campaign.EndHour)
                        {
                            var timeString = localTime.ToString("h:mm tt");
                            return BadRequest(new { error = $"Campaign cannot be sent outside calling hours ({campaign.StartHour}:00 - {campaign.EndHour}:00). Current time is {timeString} {_callingHoursSettings.TimeZone}." });
                        }
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        _logger.LogError($"Invalid timezone: {_callingHoursSettings.TimeZone}");
                        // Fall back to server time if timezone is invalid
                        var currentHour = DateTime.Now.Hour;
                        var currentDay = DateTime.Now.DayOfWeek;
                        
                        if (!campaign.IncludeWeekends && (currentDay == DayOfWeek.Saturday || currentDay == DayOfWeek.Sunday))
                        {
                            return BadRequest(new { error = $"Campaign cannot be sent on weekends. Calling hours are configured for weekdays only." });
                        }
                        
                        if (currentHour < campaign.StartHour || currentHour >= campaign.EndHour)
                        {
                            return BadRequest(new { error = $"Campaign cannot be sent outside calling hours ({campaign.StartHour}:00 - {campaign.EndHour}:00). Current server time is {currentHour}:00." });
                        }
                    }
                }

                var success = await _campaignService.SendCampaignAsync(
                    id, 
                    request?.OverrideOptIn ?? false);
                    
                if (!success)
                    return BadRequest(new { error = "Campaign cannot be sent. Please check the campaign status and try again." });

                return Ok(new { message = "Campaign is being sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending campaign {CampaignId}", id);
                return StatusCode(500, new { error = "An error occurred while sending the campaign" });
            }
        }

        [HttpPost("{id}/schedule")]
        public async Task<ActionResult> ScheduleCampaign(int id, ScheduleCampaignRequest request)
        {
            if (request.ScheduledTime <= DateTime.UtcNow)
                return BadRequest("Scheduled time must be in the future");

            var success = await _campaignService.ScheduleCampaignAsync(id, request.ScheduledTime);
            if (!success)
                return NotFound();

            return Ok(new { message = "Campaign scheduled successfully" });
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelCampaign(int id)
        {
            var success = await _campaignService.CancelCampaignAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "Campaign cancelled" });
        }

        [HttpGet("{id}/recipients")]
        public async Task<ActionResult<IEnumerable<Voter>>> GetCampaignRecipients(int id)
        {
            var recipients = await _campaignService.GetCampaignRecipientsAsync(id);
            return Ok(recipients);
        }

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<CampaignStats>> GetCampaignStats(int id)
        {
            var stats = await _campaignService.GetCampaignStatsAsync(id);
            return Ok(stats);
        }

        [HttpGet("recipient-count")]
        public async Task<ActionResult<object>> GetRecipientCount([FromQuery] RecipientCountRequest request)
        {
            try
            {
                // If campaign type is specified, return detailed count with opt-outs
                if (request.CampaignType.HasValue)
                {
                    var detailedCount = await _campaignService.GetRecipientCountWithOptOutsAsync(
                        request.CampaignType.Value,
                        request.FilterZipCodes,
                        request.FilterVoteFrequency,
                        request.FilterMinAge,
                        request.FilterMaxAge,
                        request.FilterVoterSupport,
                        request.FilterTagIds
                    );
                    return Ok(detailedCount);
                }
                
                // Otherwise return simple count for backward compatibility
                var count = await _campaignService.GetRecipientCountAsync(
                    request.FilterZipCodes,
                    request.FilterVoteFrequency,
                    request.FilterMinAge,
                    request.FilterMaxAge,
                    request.FilterVoterSupport,
                    request.FilterTagIds
                );
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recipient count");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/retry-failed")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> RetryFailedMessages(int id, SendCampaignRequest request)
        {
            try
            {
                var success = await _campaignService.RetryFailedMessagesAsync(
                    id, 
                    request?.OverrideOptIn ?? false);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to retry messages. Campaign may be sealed or have no failed messages." });
                }
                return Ok(new { message = "Retrying failed messages" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed messages for campaign {CampaignId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/duplicate")]
        public async Task<ActionResult<Campaign>> DuplicateCampaign(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var duplicatedCampaign = await _campaignService.DuplicateCampaignAsync(id, userId);
                if (duplicatedCampaign == null)
                    return NotFound(new { error = "Campaign not found" });

                return CreatedAtAction(nameof(GetCampaign), new { id = duplicatedCampaign.Id }, duplicatedCampaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating campaign {CampaignId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("check-stuck")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> CheckStuckCampaigns()
        {
            try
            {
                var resumedCampaigns = await _campaignService.CheckAndResumeStuckCampaignsAsync();
                return Ok(new 
                { 
                    resumedCount = resumedCampaigns.Count,
                    campaigns = resumedCampaigns.Select(c => new { c.Id, c.Name, c.Status })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stuck campaigns");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/force-stop")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> ForceStopCampaign(int id)
        {
            try
            {
                var success = await _campaignService.ForceStopCampaignAsync(id);
                if (!success)
                    return NotFound(new { error = "Campaign not found" });

                return Ok(new { message = "Campaign force stopped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force stopping campaign {CampaignId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CreateCampaignRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Message { get; set; }
        public CampaignType Type { get; set; }
        public string? VoiceUrl { get; set; }
        public int? VoiceRecordingId { get; set; }
        
        // Email campaign fields
        public string? EmailSubject { get; set; }
        public string? EmailHtmlContent { get; set; }
        public string? EmailPlainTextContent { get; set; }
        
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
        public List<int>? FilterTagIds { get; set; }
        
        // Calling hours settings
        public bool EnforceCallingHours { get; set; } = true;
        public int StartHour { get; set; } = 9;
        public int EndHour { get; set; } = 20;
        public bool IncludeWeekends { get; set; } = false;
        
        // Duplicate message prevention
        public bool PreventDuplicateMessages { get; set; } = false;
    }

    public class UpdateCampaignRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? VoiceUrl { get; set; }
        public int? VoiceRecordingId { get; set; }
        
        // Email campaign fields
        public string? EmailSubject { get; set; }
        public string? EmailHtmlContent { get; set; }
        public string? EmailPlainTextContent { get; set; }
        
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
        public List<int>? FilterTagIds { get; set; }
        
        // Calling hours settings
        public bool EnforceCallingHours { get; set; } = true;
        public int StartHour { get; set; } = 9;
        public int EndHour { get; set; } = 20;
        public bool IncludeWeekends { get; set; } = false;
        
        // Duplicate message prevention
        public bool PreventDuplicateMessages { get; set; } = false;
    }

    public class ScheduleCampaignRequest
    {
        public DateTime ScheduledTime { get; set; }
    }

    public class SendCampaignRequest
    {
        public bool OverrideOptIn { get; set; }
    }

    public class PreviewAudienceRequest
    {
        public string? FilterZipCodes { get; set; }
    }

    public class RecipientCountRequest
    {
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
        public List<int>? FilterTagIds { get; set; }
        public CampaignType? CampaignType { get; set; }
    }
}