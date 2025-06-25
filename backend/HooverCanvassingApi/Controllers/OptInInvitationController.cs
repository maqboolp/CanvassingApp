using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.DTOs;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Services;
using HooverCanvassingApi.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HooverCanvassingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,superadmin")]
    public class OptInInvitationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptInInvitationService _optInService;
        private readonly OptInSettings _optInSettings;
        private readonly ILogger<OptInInvitationController> _logger;

        public OptInInvitationController(
            ApplicationDbContext context,
            IOptInInvitationService optInService,
            IOptions<OptInSettings> optInSettings,
            ILogger<OptInInvitationController> logger)
        {
            _context = context;
            _optInService = optInService;
            _optInSettings = optInSettings.Value;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<SendOptInInvitationResponse>> SendOptInInvitations([FromBody] SendOptInInvitationRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"User {userId} initiating opt-in invitation send");

                List<string> voterIds;

                // If filter is provided, use it to select voters
                if (request.Filter != null && (request.VoterIds == null || !request.VoterIds.Any()))
                {
                    var query = _context.Voters
                        .Where(v => !string.IsNullOrEmpty(v.CellPhone));

                    // Apply filters
                    if (request.Filter.ZipCodes?.Any() == true)
                    {
                        query = query.Where(v => request.Filter.ZipCodes.Contains(v.Zip));
                    }

                    if (request.Filter.VoteFrequency.HasValue)
                    {
                        query = query.Where(v => v.VoteFrequency == request.Filter.VoteFrequency.Value);
                    }

                    if (request.Filter.ExcludeOptedIn == true)
                    {
                        query = query.Where(v => v.SmsConsentStatus != SmsConsentStatus.OptedIn);
                    }

                    if (request.Filter.ExcludeOptedOut == true)
                    {
                        query = query.Where(v => v.SmsConsentStatus != SmsConsentStatus.OptedOut);
                    }

                    // Apply limit if specified
                    if (request.Filter.MaxRecipients.HasValue)
                    {
                        query = query.Take(request.Filter.MaxRecipients.Value);
                    }

                    voterIds = await query.Select(v => v.LalVoterId).ToListAsync();
                }
                else
                {
                    voterIds = request.VoterIds;
                }

                if (!voterIds.Any())
                {
                    return BadRequest(new SendOptInInvitationResponse
                    {
                        Success = false,
                        Message = "No voters selected for opt-in invitations."
                    });
                }

                // Count different statuses before sending
                var voterStatuses = await _context.Voters
                    .Where(v => voterIds.Contains(v.LalVoterId))
                    .GroupBy(v => v.SmsConsentStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var alreadyOptedIn = voterStatuses.FirstOrDefault(s => s.Status == SmsConsentStatus.OptedIn)?.Count ?? 0;
                var alreadyOptedOut = voterStatuses.FirstOrDefault(s => s.Status == SmsConsentStatus.OptedOut)?.Count ?? 0;

                // Send invitations
                var successCount = await _optInService.SendOptInInvitations(voterIds, request.CustomMessage);

                // Log the action
                _logger.LogInformation($"Opt-in invitations sent: {successCount} successful out of {voterIds.Count} total");

                return Ok(new SendOptInInvitationResponse
                {
                    Success = true,
                    TotalSelected = voterIds.Count,
                    SuccessfullySent = successCount,
                    Failed = voterIds.Count - successCount - alreadyOptedIn - alreadyOptedOut,
                    AlreadyOptedIn = alreadyOptedIn,
                    AlreadyOptedOut = alreadyOptedOut,
                    Message = $"Successfully sent {successCount} opt-in invitations."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending opt-in invitations");
                return StatusCode(500, new SendOptInInvitationResponse
                {
                    Success = false,
                    Message = "An error occurred while sending opt-in invitations."
                });
            }
        }

        [HttpPost("preview")]
        public async Task<ActionResult<PreviewOptInInvitationResponse>> PreviewOptInInvitation([FromBody] SendOptInInvitationRequest request)
        {
            try
            {
                // Get message preview
                var messagePreview = request.CustomMessage ?? _optInService.GetDefaultInvitationMessage();

                // Build query based on filter or voter IDs
                IQueryable<Voter> query;

                if (request.Filter != null && (request.VoterIds == null || !request.VoterIds.Any()))
                {
                    query = _context.Voters.Where(v => !string.IsNullOrEmpty(v.CellPhone));

                    if (request.Filter.ZipCodes?.Any() == true)
                    {
                        query = query.Where(v => request.Filter.ZipCodes.Contains(v.Zip));
                    }

                    if (request.Filter.VoteFrequency.HasValue)
                    {
                        query = query.Where(v => v.VoteFrequency == request.Filter.VoteFrequency.Value);
                    }

                    if (request.Filter.ExcludeOptedIn == true)
                    {
                        query = query.Where(v => v.SmsConsentStatus != SmsConsentStatus.OptedIn);
                    }

                    if (request.Filter.ExcludeOptedOut == true)
                    {
                        query = query.Where(v => v.SmsConsentStatus != SmsConsentStatus.OptedOut);
                    }

                    if (request.Filter.MaxRecipients.HasValue)
                    {
                        query = query.Take(request.Filter.MaxRecipients.Value);
                    }
                }
                else
                {
                    query = _context.Voters.Where(v => request.VoterIds.Contains(v.LalVoterId));
                }

                var estimatedCount = await query.CountAsync();
                
                // Get sample recipients
                var sampleRecipients = await query
                    .Take(5)
                    .Select(v => new VoterPreview
                    {
                        VoterId = v.LalVoterId,
                        Name = $"{v.FirstName} {v.LastName}",
                        PhoneNumber = v.CellPhone ?? "No phone",
                        ConsentStatus = v.SmsConsentStatus.ToString()
                    })
                    .ToListAsync();

                return Ok(new PreviewOptInInvitationResponse
                {
                    MessagePreview = messagePreview,
                    EstimatedRecipients = estimatedCount,
                    SampleRecipients = sampleRecipients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing opt-in invitation");
                return StatusCode(500, "An error occurred while previewing the invitation.");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetOptInStats()
        {
            try
            {
                var stats = await _context.Voters
                    .Where(v => !string.IsNullOrEmpty(v.CellPhone))
                    .GroupBy(v => v.SmsConsentStatus)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync();

                var totalWithPhone = await _context.Voters
                    .CountAsync(v => !string.IsNullOrEmpty(v.CellPhone));

                var recentOptIns = await _context.ConsentRecords
                    .Where(cr => cr.Action == ConsentAction.OptIn && cr.Timestamp >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();

                var recentOptOuts = await _context.ConsentRecords
                    .Where(cr => cr.Action == ConsentAction.OptOut && cr.Timestamp >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();

                return Ok(new
                {
                    totalVotersWithPhone = totalWithPhone,
                    consentStatusBreakdown = stats,
                    recentActivity = new
                    {
                        optInsLast7Days = recentOptIns,
                        optOutsLast7Days = recentOptOuts
                    },
                    settings = new
                    {
                        campaignPhone = _optInSettings.CampaignPhone,
                        optInWebsiteUrl = _optInSettings.OptInWebsiteUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opt-in statistics");
                return StatusCode(500, "An error occurred while retrieving statistics.");
            }
        }
    }
}