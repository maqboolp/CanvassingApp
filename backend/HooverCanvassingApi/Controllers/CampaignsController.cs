using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
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
                Message = request.Message,
                Type = request.Type,
                CreatedById = userId,
                VoiceUrl = request.VoiceUrl,
                FilterZipCodes = request.FilterZipCodes,
                FilterVoteFrequency = request.FilterVoteFrequency,
                FilterMinAge = request.FilterMinAge,
                FilterMaxAge = request.FilterMaxAge,
                FilterVoterSupport = request.FilterVoterSupport
            };

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
                return BadRequest("Can only update draft campaigns");

            campaign.Name = request.Name;
            campaign.Message = request.Message;
            campaign.VoiceUrl = request.VoiceUrl;
            campaign.FilterZipCodes = request.FilterZipCodes;
            campaign.FilterVoteFrequency = request.FilterVoteFrequency;
            campaign.FilterMinAge = request.FilterMinAge;
            campaign.FilterMaxAge = request.FilterMaxAge;
            campaign.FilterVoterSupport = request.FilterVoterSupport;

            var updatedCampaign = await _campaignService.UpdateCampaignAsync(campaign);
            return Ok(updatedCampaign);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCampaign(int id)
        {
            var success = await _campaignService.DeleteCampaignAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPost("{id}/send")]
        public async Task<ActionResult> SendCampaign(int id)
        {
            var success = await _campaignService.SendCampaignAsync(id);
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
            // Create a temporary campaign to use the filtering logic
            var tempCampaign = new Campaign
            {
                FilterZipCodes = request.FilterZipCodes,
                FilterVoteFrequency = request.FilterVoteFrequency,
                FilterMinAge = request.FilterMinAge,
                FilterMaxAge = request.FilterMaxAge,
                FilterVoterSupport = request.FilterVoterSupport
            };

            var recipients = await _campaignService.GetCampaignRecipientsAsync(-1); // Fake ID, service will use filters
            return Ok(recipients.Count());
        }
    }

    public class CreateCampaignRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public CampaignType Type { get; set; }
        public string? VoiceUrl { get; set; }
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
    }

    public class UpdateCampaignRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? VoiceUrl { get; set; }
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
    }

    public class ScheduleCampaignRequest
    {
        public DateTime ScheduledTime { get; set; }
    }

    public class RecipientCountRequest
    {
        public string? FilterZipCodes { get; set; }
        public VoteFrequency? FilterVoteFrequency { get; set; }
        public int? FilterMinAge { get; set; }
        public int? FilterMaxAge { get; set; }
        public VoterSupport? FilterVoterSupport { get; set; }
    }
}