using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HooverCanvassingApi.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly ILogger<CampaignService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CampaignService(
            ApplicationDbContext context,
            ITwilioService twilioService,
            ILogger<CampaignService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _twilioService = twilioService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<Campaign> CreateCampaignAsync(Campaign campaign)
        {
            campaign.CreatedAt = DateTime.UtcNow;
            campaign.Status = CampaignStatus.Draft;
            
            // Calculate total recipients for draft campaigns
            var recipients = await GetFilteredVotersAsync(campaign);
            campaign.TotalRecipients = recipients.Count(v => !string.IsNullOrEmpty(v.CellPhone));
            
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Campaign '{campaign.Name}' created with ID {campaign.Id} and {campaign.TotalRecipients} recipients");
            
            // Clear navigation properties to avoid circular reference in JSON serialization
            campaign.VoiceRecording = null;
            campaign.Messages = new List<CampaignMessage>();
            
            return campaign;
        }

        public async Task<Campaign?> GetCampaignAsync(int id)
        {
            return await _context.Campaigns
                .Include(c => c.Messages)
                .ThenInclude(m => m.Voter)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Campaign>> GetCampaignsAsync()
        {
            return await _context.Campaigns
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Campaign> UpdateCampaignAsync(Campaign campaign)
        {
            // Recalculate total recipients if campaign is still in draft
            if (campaign.Status == CampaignStatus.Draft)
            {
                var recipients = await GetFilteredVotersAsync(campaign);
                campaign.TotalRecipients = recipients.Count(v => !string.IsNullOrEmpty(v.CellPhone));
            }
            
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();
            
            // Clear navigation properties to avoid circular reference in JSON serialization
            campaign.VoiceRecording = null;
            campaign.Messages = new List<CampaignMessage>();
            
            return campaign;
        }

        public async Task<bool> DeleteCampaignAsync(int id)
        {
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null)
                return false;

            if (campaign.Status == CampaignStatus.Sending)
            {
                _logger.LogWarning($"Cannot delete campaign {id} - currently sending");
                return false;
            }

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendCampaignAsync(int campaignId, bool overrideOptIn = false)
        {
            var campaign = await GetCampaignAsync(campaignId);
            if (campaign == null)
                return false;

            if (campaign.Status != CampaignStatus.Draft && campaign.Status != CampaignStatus.Scheduled)
            {
                _logger.LogWarning($"Campaign {campaignId} cannot be sent - status: {campaign.Status}");
                return false;
            }

            campaign.Status = CampaignStatus.Sending;
            campaign.SentAt = DateTime.UtcNow;
            
            // Get eligible voters based on campaign filters
            var recipients = await GetFilteredVotersAsync(campaign);
            
            // Create campaign messages for each recipient
            var campaignMessages = new List<CampaignMessage>();
            foreach (var voter in recipients)
            {
                if (!string.IsNullOrEmpty(voter.CellPhone))
                {
                    campaignMessages.Add(new CampaignMessage
                    {
                        CampaignId = campaign.Id,
                        VoterId = voter.LalVoterId,
                        RecipientPhone = voter.CellPhone,
                        Status = MessageStatus.Pending
                    });
                }
            }

            _context.CampaignMessages.AddRange(campaignMessages);
            campaign.TotalRecipients = campaignMessages.Count;
            campaign.PendingDeliveries = campaignMessages.Count;
            
            await _context.SaveChangesAsync();

            // Send messages in background with new scope
            _logger.LogInformation($"Starting background task for campaign {campaignId} with overrideOptIn={overrideOptIn}");
            _ = Task.Run(async () => 
            {
                _logger.LogInformation($"Background task started for campaign {campaignId}");
                await ProcessCampaignMessagesWithScopeAsync(campaignId, overrideOptIn);
            });

            _logger.LogInformation($"Campaign {campaignId} started with {campaignMessages.Count} recipients");
            return true;
        }

        public async Task<bool> ScheduleCampaignAsync(int campaignId, DateTime scheduledTime)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
                return false;

            campaign.Status = CampaignStatus.Scheduled;
            campaign.ScheduledTime = scheduledTime;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelCampaignAsync(int campaignId)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
                return false;

            campaign.Status = CampaignStatus.Cancelled;
            await _context.SaveChangesAsync();
            
            // Cancel pending messages
            var pendingMessages = await _context.CampaignMessages
                .Where(cm => cm.CampaignId == campaignId && cm.Status == MessageStatus.Pending)
                .ToListAsync();
            
            foreach (var message in pendingMessages)
            {
                message.Status = MessageStatus.Cancelled;
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Voter>> GetCampaignRecipientsAsync(int campaignId)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
                return Enumerable.Empty<Voter>();

            return await GetFilteredVotersAsync(campaign);
        }

        public async Task<CampaignStats> GetCampaignStatsAsync(int campaignId)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
                return new CampaignStats();

            var stats = new CampaignStats
            {
                TotalRecipients = campaign.Messages.Count,
                Sent = campaign.Messages.Count(m => m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered),
                Delivered = campaign.Messages.Count(m => m.Status == MessageStatus.Delivered),
                Failed = campaign.Messages.Count(m => m.Status == MessageStatus.Failed),
                Pending = campaign.Messages.Count(m => m.Status == MessageStatus.Pending || m.Status == MessageStatus.Queued),
                TotalCost = campaign.Messages.Where(m => m.Cost.HasValue).Sum(m => m.Cost.Value)
            };

            var statusGroups = campaign.Messages
                .GroupBy(m => m.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            stats.StatusBreakdown = statusGroups;
            return stats;
        }

        public async Task<int> PreviewAudienceCountAsync(string? filterZipCodes)
        {
            var query = _context.Voters.AsQueryable();

            // Filter by zip codes
            if (!string.IsNullOrEmpty(filterZipCodes))
            {
                var zipCodes = JsonSerializer.Deserialize<string[]>(filterZipCodes);
                if (zipCodes != null && zipCodes.Length > 0)
                {
                    query = query.Where(v => zipCodes.Contains(v.Zip));
                }
            }

            // Only count voters with valid cell phone numbers
            query = query.Where(v => !string.IsNullOrEmpty(v.CellPhone));

            return await query.CountAsync();
        }

        public async Task<int> GetRecipientCountAsync(string? filterZipCodes, VoteFrequency? filterVoteFrequency, int? filterMinAge, int? filterMaxAge, VoterSupport? filterVoterSupport, List<int>? filterTagIds = null)
        {
            var query = _context.Voters.AsQueryable();

            // Filter by zip codes
            if (!string.IsNullOrEmpty(filterZipCodes))
            {
                var zipCodes = JsonSerializer.Deserialize<string[]>(filterZipCodes);
                if (zipCodes != null && zipCodes.Length > 0)
                {
                    query = query.Where(v => zipCodes.Contains(v.Zip));
                }
            }

            // Filter by vote frequency
            if (filterVoteFrequency.HasValue)
            {
                query = query.Where(v => v.VoteFrequency == filterVoteFrequency.Value);
            }

            // Filter by age range
            if (filterMinAge.HasValue)
            {
                query = query.Where(v => v.Age >= filterMinAge.Value);
            }

            if (filterMaxAge.HasValue)
            {
                query = query.Where(v => v.Age <= filterMaxAge.Value);
            }

            // Filter by voter support
            if (filterVoterSupport.HasValue)
            {
                query = query.Where(v => v.VoterSupport == filterVoterSupport.Value);
            }

            // Filter by tags
            if (filterTagIds != null && filterTagIds.Any())
            {
                query = query.Where(v => v.TagAssignments.Any(ta => filterTagIds.Contains(ta.TagId)));
            }

            // Only count voters with valid cell phone numbers
            query = query.Where(v => !string.IsNullOrEmpty(v.CellPhone));

            return await query.CountAsync();
        }

        public async Task<IEnumerable<string>> GetAvailableZipCodesAsync()
        {
            return await _context.Voters
                .Where(v => !string.IsNullOrEmpty(v.Zip) && !string.IsNullOrEmpty(v.CellPhone))
                .Select(v => v.Zip)
                .Distinct()
                .OrderBy(z => z)
                .ToListAsync();
        }

        public async Task ProcessScheduledCampaignsAsync()
        {
            var scheduledCampaigns = await _context.Campaigns
                .Where(c => c.Status == CampaignStatus.Scheduled && 
                           c.ScheduledTime.HasValue && 
                           c.ScheduledTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var campaign in scheduledCampaigns)
            {
                await SendCampaignAsync(campaign.Id);
            }
        }

        private async Task<List<Voter>> GetFilteredVotersAsync(Campaign campaign)
        {
            var query = _context.Voters.AsQueryable();

            // Filter by zip codes
            if (!string.IsNullOrEmpty(campaign.FilterZipCodes))
            {
                var zipCodes = JsonSerializer.Deserialize<string[]>(campaign.FilterZipCodes);
                if (zipCodes != null && zipCodes.Length > 0)
                {
                    query = query.Where(v => zipCodes.Contains(v.Zip));
                }
            }

            // Filter by vote frequency
            if (campaign.FilterVoteFrequency.HasValue)
            {
                query = query.Where(v => v.VoteFrequency == campaign.FilterVoteFrequency.Value);
            }

            // Filter by age range
            if (campaign.FilterMinAge.HasValue)
            {
                query = query.Where(v => v.Age >= campaign.FilterMinAge.Value);
            }

            if (campaign.FilterMaxAge.HasValue)
            {
                query = query.Where(v => v.Age <= campaign.FilterMaxAge.Value);
            }

            // Filter by voter support
            if (campaign.FilterVoterSupport.HasValue)
            {
                query = query.Where(v => v.VoterSupport == campaign.FilterVoterSupport.Value);
            }

            // Filter by tags
            if (!string.IsNullOrEmpty(campaign.FilterTags))
            {
                var tagIds = JsonSerializer.Deserialize<int[]>(campaign.FilterTags);
                if (tagIds != null && tagIds.Length > 0)
                {
                    query = query.Where(v => v.TagAssignments.Any(ta => tagIds.Contains(ta.TagId)));
                }
            }

            // Only include voters with phone numbers
            query = query.Where(v => !string.IsNullOrEmpty(v.CellPhone));

            return await query.ToListAsync();
        }

        private async Task ProcessCampaignMessagesAsync(int campaignId)
        {
            try
            {
                var campaign = await GetCampaignAsync(campaignId);
                if (campaign == null)
                    return;

                var pendingMessages = campaign.Messages
                    .Where(m => m.Status == MessageStatus.Pending)
                    .OrderBy(m => m.Id)
                    .ToList();

                var successfulDeliveries = 0;
                var failedDeliveries = 0;

                if (campaign.Type == CampaignType.SMS)
                {
                    // Use bulk SMS for better performance
                    var smsMessages = pendingMessages
                        .Select(m => (m.RecipientPhone, campaign.Message, m.Id))
                        .ToList();

                    var results = await _twilioService.SendBulkSmsAsync(smsMessages);
                    
                    // Process results and update voter stats
                    for (int i = 0; i < results.Count; i++)
                    {
                        if (results[i])
                        {
                            successfulDeliveries++;
                            // Update voter campaign tracking statistics
                            await UpdateVoterCampaignStatsAsync(pendingMessages[i].VoterId, campaign.Id, campaign.Type);
                        }
                        else
                        {
                            failedDeliveries++;
                        }
                    }
                }
                else
                {
                    // Keep individual processing for robo calls
                    foreach (var message in pendingMessages)
                    {
                        try
                        {
                            // Add small delay to respect rate limits
                            await Task.Delay(1000);

                            var success = await _twilioService.MakeRoboCallAsync(
                                message.RecipientPhone, 
                                campaign.VoiceUrl!, 
                                message.Id);

                            if (success)
                            {
                                successfulDeliveries++;
                                // Update voter campaign tracking statistics
                                await UpdateVoterCampaignStatsAsync(message.VoterId, campaign.Id, campaign.Type);
                            }
                            else
                                failedDeliveries++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing message {message.Id}");
                            failedDeliveries++;
                        }
                    }
                }

                // Update campaign status
                campaign.Status = CampaignStatus.Completed;
                campaign.SuccessfulDeliveries = successfulDeliveries;
                campaign.FailedDeliveries = failedDeliveries;
                campaign.PendingDeliveries = 0;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign {campaignId} completed: {successfulDeliveries} successful, {failedDeliveries} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing campaign {campaignId}");
                
                // Mark campaign as failed
                var campaign = await _context.Campaigns.FindAsync(campaignId);
                if (campaign != null)
                {
                    campaign.Status = CampaignStatus.Failed;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task UpdateVoterCampaignStatsAsync(string voterId, int campaignId, CampaignType campaignType)
        {
            var voter = await _context.Voters.FindAsync(voterId);
            if (voter == null)
                return;

            var now = DateTime.UtcNow;
            
            // Update general campaign contact tracking
            voter.LastCampaignContactAt = now;
            voter.LastCampaignId = campaignId;
            voter.TotalCampaignContacts++;

            // Update specific communication type tracking
            if (campaignType == CampaignType.SMS)
            {
                voter.LastSmsAt = now;
                voter.LastSmsCampaignId = campaignId;
                voter.SmsCount++;
            }
            else if (campaignType == CampaignType.RoboCall)
            {
                voter.LastCallAt = now;
                voter.LastCallCampaignId = campaignId;
                voter.CallCount++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task ProcessCampaignMessagesWithScopeAsync(int campaignId, bool overrideOptIn = false)
        {
            _logger.LogInformation($"ProcessCampaignMessagesWithScopeAsync called for campaign {campaignId} with overrideOptIn={overrideOptIn}");
            
            try
            {
                _logger.LogInformation($"Creating scope for campaign {campaignId}");
                using var scope = _serviceScopeFactory.CreateScope();
                _logger.LogInformation($"Scope created, getting services for campaign {campaignId}");
                
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedTwilioService = scope.ServiceProvider.GetRequiredService<ITwilioService>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<CampaignService>>();

                scopedLogger.LogInformation($"Scoped services created for campaign {campaignId}");

                try
                {
                    var campaign = await scopedContext.Campaigns
                        .Include(c => c.Messages)
                        .ThenInclude(m => m.Voter)
                        .FirstOrDefaultAsync(c => c.Id == campaignId);

                    if (campaign == null)
                        return;

                    var pendingMessages = campaign.Messages
                        .Where(m => m.Status == MessageStatus.Pending)
                        .OrderBy(m => m.Id)
                        .ToList();

                    var successfulDeliveries = 0;
                    var failedDeliveries = 0;

                    if (campaign.Type == CampaignType.SMS)
                    {
                        // Use bulk SMS for better performance
                        var smsMessages = pendingMessages
                            .Select(m => (m.RecipientPhone, campaign.Message, m.Id))
                            .ToList();

                        var results = await scopedTwilioService.SendBulkSmsAsync(smsMessages, overrideOptIn);
                        
                        // Process results and update voter stats
                        for (int i = 0; i < results.Count; i++)
                        {
                            if (results[i])
                            {
                                successfulDeliveries++;
                                // Update voter campaign tracking statistics
                                await UpdateVoterCampaignStatsWithContextAsync(scopedContext, pendingMessages[i].VoterId, campaign.Id, campaign.Type);
                            }
                            else
                            {
                                failedDeliveries++;
                            }
                        }
                    }
                    else
                    {
                        // Keep individual processing for robo calls
                        foreach (var message in pendingMessages)
                        {
                            try
                            {
                                // Add small delay to respect rate limits
                                await Task.Delay(1000);

                                var success = await scopedTwilioService.MakeRoboCallAsync(
                                    message.RecipientPhone, 
                                    campaign.VoiceUrl!, 
                                    message.Id);

                                if (success)
                                {
                                    successfulDeliveries++;
                                    // Update voter campaign tracking statistics
                                    await UpdateVoterCampaignStatsWithContextAsync(scopedContext, message.VoterId, campaign.Id, campaign.Type);
                                }
                                else
                                    failedDeliveries++;
                            }
                            catch (Exception ex)
                            {
                                scopedLogger.LogError(ex, $"Error processing message {message.Id}");
                                failedDeliveries++;
                            }
                        }
                    }

                    // Update campaign status
                    campaign.Status = CampaignStatus.Completed;
                    campaign.SuccessfulDeliveries = successfulDeliveries;
                    campaign.FailedDeliveries = failedDeliveries;
                    campaign.PendingDeliveries = 0;

                    await scopedContext.SaveChangesAsync();

                    scopedLogger.LogInformation($"Campaign {campaignId} completed: {successfulDeliveries} successful, {failedDeliveries} failed");
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex, $"Error processing campaign {campaignId}");
                    
                    // Mark campaign as failed
                    var campaign = await scopedContext.Campaigns.FindAsync(campaignId);
                    if (campaign != null)
                    {
                        campaign.Status = CampaignStatus.Failed;
                        await scopedContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating scope for campaign {campaignId}");
            }
        }

        private async Task UpdateVoterCampaignStatsWithContextAsync(ApplicationDbContext context, string voterId, int campaignId, CampaignType campaignType)
        {
            var voter = await context.Voters.FindAsync(voterId);
            if (voter == null)
                return;

            var now = DateTime.UtcNow;
            
            // Update general campaign contact tracking
            voter.LastCampaignContactAt = now;
            voter.LastCampaignId = campaignId;
            voter.TotalCampaignContacts++;

            // Update specific communication type tracking
            if (campaignType == CampaignType.SMS)
            {
                voter.LastSmsAt = now;
                voter.LastSmsCampaignId = campaignId;
                voter.SmsCount++;
            }
            else if (campaignType == CampaignType.RoboCall)
            {
                voter.LastCallAt = now;
                voter.LastCallCampaignId = campaignId;
                voter.CallCount++;
            }

            await context.SaveChangesAsync();
        }

        public async Task<bool> RetryFailedMessagesAsync(int campaignId, bool overrideOptIn = false)
        {
            try
            {
                var campaign = await _context.Campaigns
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == campaignId);

                if (campaign == null)
                    return false;

                // Don't retry if campaign is sealed
                if (campaign.Status == CampaignStatus.Sealed)
                {
                    _logger.LogWarning($"Cannot retry messages for sealed campaign {campaignId}");
                    return false;
                }

                // Get failed messages
                var failedMessages = campaign.Messages
                    .Where(m => m.Status == MessageStatus.Failed || 
                               m.Status == MessageStatus.Undelivered || 
                               m.Status == MessageStatus.NoAnswer ||
                               m.Status == MessageStatus.Busy)
                    .ToList();

                if (!failedMessages.Any())
                {
                    _logger.LogInformation($"No failed messages to retry for campaign {campaignId}");
                    // Check if we should seal the campaign
                    await SealCampaignIfCompleteAsync(campaignId);
                    return true;
                }

                _logger.LogInformation($"Retrying {failedMessages.Count} failed messages for campaign {campaignId}");

                // Update campaign status
                campaign.Status = CampaignStatus.Sending;
                campaign.PendingDeliveries = failedMessages.Count;
                await _context.SaveChangesAsync();

                // Prepare messages for retry
                var messagesToRetry = failedMessages.Select(m => new
                {
                    PhoneNumber = m.RecipientPhone,
                    Message = campaign.Message,
                    CampaignMessageId = m.Id,
                    VoiceUrl = campaign.VoiceUrl
                }).ToList();

                // Retry based on campaign type
                if (campaign.Type == CampaignType.SMS)
                {
                    var messageList = messagesToRetry.Select(m => 
                        (m.PhoneNumber, m.Message, m.CampaignMessageId)).ToList();
                    
                    await _twilioService.SendBulkSmsAsync(messageList, overrideOptIn);
                }
                else if (campaign.Type == CampaignType.RoboCall)
                {
                    // Retry robo calls one by one
                    foreach (var msg in messagesToRetry)
                    {
                        if (!string.IsNullOrEmpty(msg.VoiceUrl))
                        {
                            await _twilioService.MakeRoboCallAsync(msg.PhoneNumber, msg.VoiceUrl, msg.CampaignMessageId);
                        }
                    }
                }

                // Update retry count for failed messages
                foreach (var msg in failedMessages)
                {
                    msg.RetryCount++;
                    msg.Status = MessageStatus.Queued;
                    msg.ErrorMessage = null;
                }
                await _context.SaveChangesAsync();

                // Schedule status update check
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(2));
                    await UpdateCampaignStatsAsync(campaignId);
                    await SealCampaignIfCompleteAsync(campaignId);
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrying failed messages for campaign {campaignId}");
                return false;
            }
        }

        private async Task UpdateCampaignStatsAsync(int campaignId)
        {
            try
            {
                var campaign = await _context.Campaigns
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == campaignId);

                if (campaign == null)
                    return;

                // Update stats based on message statuses
                campaign.SuccessfulDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Delivered || 
                    m.Status == MessageStatus.Sent ||
                    m.Status == MessageStatus.Completed);
                    
                campaign.FailedDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Failed || 
                    m.Status == MessageStatus.Undelivered ||
                    m.Status == MessageStatus.NoAnswer ||
                    m.Status == MessageStatus.Busy);
                    
                campaign.PendingDeliveries = campaign.Messages.Count(m => 
                    m.Status == MessageStatus.Pending ||
                    m.Status == MessageStatus.Queued ||
                    m.Status == MessageStatus.Sending);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating campaign stats for campaign {campaignId}");
            }
        }

        public async Task<bool> SealCampaignIfCompleteAsync(int campaignId)
        {
            try
            {
                var campaign = await _context.Campaigns
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == campaignId);

                if (campaign == null || campaign.Status == CampaignStatus.Sealed)
                    return false;

                // Check if all messages are in a final state
                var hasUnfinishedMessages = campaign.Messages.Any(m => 
                    m.Status == MessageStatus.Pending ||
                    m.Status == MessageStatus.Queued ||
                    m.Status == MessageStatus.Sending);

                if (!hasUnfinishedMessages)
                {
                    // Check if all messages are successful
                    var failedCount = campaign.Messages.Count(m => 
                        m.Status == MessageStatus.Failed || 
                        m.Status == MessageStatus.Undelivered ||
                        m.Status == MessageStatus.NoAnswer ||
                        m.Status == MessageStatus.Busy);

                    if (failedCount == 0)
                    {
                        // All messages successful - seal the campaign
                        campaign.Status = CampaignStatus.Sealed;
                        _logger.LogInformation($"Campaign {campaignId} sealed - all messages delivered successfully");
                    }
                    else
                    {
                        // Has failed messages - mark as completed but not sealed
                        campaign.Status = CampaignStatus.Completed;
                        _logger.LogInformation($"Campaign {campaignId} completed with {failedCount} failed messages");
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sealing campaign {campaignId}");
                return false;
            }
        }

        public async Task<VoiceRecording?> GetVoiceRecordingAsync(int id)
        {
            return await _context.VoiceRecordings.FindAsync(id);
        }
    }
}