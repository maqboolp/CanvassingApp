using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly ILogger<CampaignsController> _logger;

        public CampaignsController(ICampaignService campaignService, ILogger<CampaignsController> logger)
        {
            _campaignService = campaignService;
            _logger = logger;
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
            campaign.FilterZipCodes = request.FilterZipCodes;
            campaign.FilterVoteFrequency = request.FilterVoteFrequency;
            campaign.FilterMinAge = request.FilterMinAge;
            campaign.FilterMaxAge = request.FilterMaxAge;
            campaign.FilterVoterSupport = request.FilterVoterSupport;
            
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
            var success = await _campaignService.SendCampaignAsync(
                id, 
                request?.OverrideOptIn ?? false,
                request?.BatchSize,
                request?.BatchDelayMinutes);
            if (!success)
                return BadRequest("Campaign cannot be sent");

            return Ok(new { message = "Campaign is being sent" });
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
        public async Task<ActionResult<int>> GetRecipientCount([FromQuery] RecipientCountRequest request)
        {
            try
            {
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
                    request?.OverrideOptIn ?? false,
                    request?.BatchSize,
                    request?.BatchDelayMinutes);
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
        public int? BatchSize { get; set; }
        public int? BatchDelayMinutes { get; set; }
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
    }
}