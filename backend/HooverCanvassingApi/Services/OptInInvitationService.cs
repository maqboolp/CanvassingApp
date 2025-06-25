using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HooverCanvassingApi.Services
{
    public interface IOptInInvitationService
    {
        Task<int> SendOptInInvitations(List<string> voterIds, string? customMessage = null);
        string GetDefaultInvitationMessage();
    }

    public class OptInInvitationService : IOptInInvitationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly ILogger<OptInInvitationService> _logger;
        private readonly OptInSettings _optInSettings;

        public OptInInvitationService(
            ApplicationDbContext context,
            ITwilioService twilioService,
            ILogger<OptInInvitationService> logger,
            IOptions<OptInSettings> optInSettings)
        {
            _context = context;
            _twilioService = twilioService;
            _logger = logger;
            _optInSettings = optInSettings.Value;
        }

        public string GetDefaultInvitationMessage()
        {
            return _optInSettings.FormatMessage(_optInSettings.DefaultInvitationMessage);
        }

        public async Task<int> SendOptInInvitations(List<string> voterIds, string? customMessage = null)
        {
            var message = customMessage ?? GetDefaultInvitationMessage();
            var successCount = 0;

            // Get voters who haven't opted in or out
            var voters = await _context.Voters
                .Where(v => voterIds.Contains(v.LalVoterId) 
                    && !string.IsNullOrEmpty(v.CellPhone)
                    && v.SmsConsentStatus == SmsConsentStatus.Unknown)
                .ToListAsync();

            _logger.LogInformation($"Sending opt-in invitations to {voters.Count} voters");

            foreach (var voter in voters)
            {
                try
                {
                    // Send the invitation without opt-in check (since this is the invitation itself)
                    var sent = await _twilioService.SendSmsAsync(voter.CellPhone!, message);
                    
                    if (sent)
                    {
                        successCount++;
                        
                        // Create consent record for the invitation
                        var consentRecord = new ConsentRecord
                        {
                            VoterId = voter.LalVoterId,
                            Action = ConsentAction.OptInReminder,
                            Method = ConsentMethod.TextMessage,
                            Timestamp = DateTime.UtcNow,
                            Source = "Campaign Invitation",
                            Details = "Opt-in invitation sent"
                        };
                        _context.ConsentRecords.Add(consentRecord);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send opt-in invitation to voter {voter.LalVoterId}");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully sent {successCount} out of {voters.Count} opt-in invitations");
            return successCount;
        }
    }
}