using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HooverCanvassingApi.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITwilioService _twilioService;
        private readonly ILogger<CampaignService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBackgroundServiceMonitor _backgroundMonitor;
        private readonly CallingHoursSettings _callingHoursSettings;

        public CampaignService(
            ApplicationDbContext context,
            ITwilioService twilioService,
            ILogger<CampaignService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundServiceMonitor backgroundMonitor,
            IOptions<CallingHoursSettings> callingHoursSettings)
        {
            _context = context;
            _twilioService = twilioService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _backgroundMonitor = backgroundMonitor;
            _callingHoursSettings = callingHoursSettings.Value;
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

        public async Task<bool> ForceStopCampaignAsync(int campaignId)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == campaignId);
                
            if (campaign == null)
                return false;

            _logger.LogWarning($"Force stopping campaign {campaignId} - {campaign.Name}");

            // Mark campaign as failed
            campaign.Status = CampaignStatus.Failed;

            // Mark all pending messages as failed
            var pendingMessages = campaign.Messages
                .Where(m => m.Status == MessageStatus.Pending || 
                           m.Status == MessageStatus.Sending ||
                           m.Status == MessageStatus.Queued)
                .ToList();

            foreach (var message in pendingMessages)
            {
                message.Status = MessageStatus.Failed;
                message.FailedAt = DateTime.UtcNow;
                message.ErrorMessage = "Campaign was force stopped";
            }

            // Update stats
            campaign.FailedDeliveries = campaign.Messages.Count(m => m.Status == MessageStatus.Failed);
            campaign.PendingDeliveries = 0;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Force stopped campaign {campaignId}. Marked {pendingMessages.Count} messages as failed.");
            
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
            
            // Check calling hours for robocalls BEFORE starting
            if (campaign.Type == CampaignType.RoboCall && campaign.EnforceCallingHours)
            {
                var isWithinHours = IsWithinCallingHours(campaign);
                _logger.LogInformation($"Robocall campaign {campaignId} - EnforceCallingHours: {campaign.EnforceCallingHours}, " +
                    $"Hours: {campaign.StartHour}-{campaign.EndHour}, IncludeWeekends: {campaign.IncludeWeekends}, " +
                    $"CurrentlyWithinHours: {isWithinHours}");
                    
                if (!isWithinHours)
                {
                    _logger.LogWarning($"Robocall campaign {campaignId} BLOCKED - outside calling hours. " +
                        $"Current time is outside {campaign.StartHour}:00 - {campaign.EndHour}:00");
                    return false;
                }
            }

            campaign.Status = CampaignStatus.Sending;
            campaign.SentAt = DateTime.UtcNow;
            
            // Get eligible voters based on campaign filters
            var recipients = await GetFilteredVotersAsync(campaign);
            
            // Create campaign messages for each recipient
            var campaignMessages = new List<CampaignMessage>();
            
            // Get list of voters who already received this exact message/voice URL (if duplicate prevention is enabled)
            HashSet<string> duplicateRecipients = new HashSet<string>();
            if (campaign.PreventDuplicateMessages)
            {
                // Define statuses that indicate successful delivery (exclude these from retries)
                var successfulStatuses = new[] { 
                    MessageStatus.Sent, 
                    MessageStatus.Delivered, 
                    MessageStatus.Completed 
                };
                
                var existingMessages = await _context.CampaignMessages
                    .Include(cm => cm.Campaign)
                    .Where(cm => cm.Campaign.Type == campaign.Type && 
                               (campaign.Type == CampaignType.SMS ? cm.Campaign.Message == campaign.Message :
                                cm.Campaign.VoiceUrl == campaign.VoiceUrl) &&
                               successfulStatuses.Contains(cm.Status)) // Only exclude successfully delivered messages
                    .Select(cm => cm.RecipientPhone)
                    .ToListAsync();
                
                duplicateRecipients = new HashSet<string>(existingMessages);
                _logger.LogInformation($"Duplicate prevention enabled: Found {duplicateRecipients.Count} recipients who successfully received this message (failed/busy/no-answer can be retried)");
            }
            
            foreach (var voter in recipients)
            {
                if (!string.IsNullOrEmpty(voter.CellPhone))
                {
                    // Skip if duplicate prevention is enabled and this voter already received the same message
                    if (campaign.PreventDuplicateMessages && duplicateRecipients.Contains(voter.CellPhone))
                    {
                        _logger.LogDebug($"Skipping duplicate recipient: {voter.CellPhone}");
                        continue;
                    }
                    
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

            var totalRecipients = campaign.Messages.Count;
            var failed = campaign.Messages.Count(m => m.Status == MessageStatus.Failed);
            
            var sent = campaign.Messages.Count(m => m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered);
            
            var stats = new CampaignStats
            {
                TotalRecipients = totalRecipients,
                Sent = sent,
                Delivered = campaign.Messages.Count(m => m.Status == MessageStatus.Delivered),
                Failed = failed,
                Pending = campaign.Messages.Count(m => m.Status == MessageStatus.Pending || m.Status == MessageStatus.Queued),
                Remaining = totalRecipients - sent - failed, // Messages not yet processed
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
            var operationName = $"Campaign_{campaignId}_Processing";
            _logger.LogInformation($"ProcessCampaignMessagesWithScopeAsync called for campaign {campaignId} with overrideOptIn={overrideOptIn}");
            
            try
            {
                _backgroundMonitor.StartOperation(operationName, $"Processing campaign {campaignId}");
                
                // Get initial campaign data and pending messages
                List<int> pendingMessageIds;
                CampaignType campaignType;
                string? campaignMessage = null;
                string? voiceUrl = null;
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var campaign = await context.Campaigns
                        .Include(c => c.Messages)
                        .FirstOrDefaultAsync(c => c.Id == campaignId);

                    if (campaign == null)
                    {
                        _logger.LogWarning($"Campaign {campaignId} not found");
                        return;
                    }

                    // Store campaign data to avoid keeping context alive
                    campaignType = campaign.Type;
                    campaignMessage = campaign.Message;
                    voiceUrl = campaign.VoiceUrl;
                    pendingMessageIds = campaign.Messages
                        .Where(m => m.Status == MessageStatus.Pending)
                        .OrderBy(m => m.Id)
                        .Select(m => m.Id)
                        .ToList();
                }

                var totalMessages = pendingMessageIds.Count;
                _logger.LogInformation($"Processing {totalMessages} messages for campaign {campaignId}");

                if (campaignType == CampaignType.SMS)
                {
                    await ProcessSmsCampaignAsync(campaignId, pendingMessageIds, campaignMessage!, overrideOptIn);
                }
                else
                {
                    // Validate voice URL for robocalls
                    if (string.IsNullOrEmpty(voiceUrl))
                    {
                        _logger.LogError($"Campaign {campaignId} has no voice URL configured - cannot process robocalls");
                        await MarkCampaignAsFailedAsync(campaignId);
                        _backgroundMonitor.FailOperation(operationName, "No voice URL configured");
                        return;
                    }
                    
                    await ProcessRobocallCampaignAsync(campaignId, pendingMessageIds, voiceUrl, overrideOptIn);
                }

                _backgroundMonitor.CompleteOperation(operationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing campaign {campaignId}");
                _backgroundMonitor.FailOperation(operationName, ex.Message);
                
                // Mark campaign as failed
                await MarkCampaignAsFailedAsync(campaignId);
            }
        }
        
        private async Task ProcessRobocallCampaignAsync(int campaignId, List<int> messageIds, string voiceUrl, bool overrideOptIn)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var twilioService = scope.ServiceProvider.GetRequiredService<ITwilioService>();
            
            // Load all messages
            var messages = await context.CampaignMessages
                .Include(m => m.Voter)
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();
            
            var successes = 0;
            var failures = 0;
            
            _logger.LogInformation($"Processing {messages.Count} robocalls for campaign {campaignId}");
            
            // Process calls concurrently with reduced connection usage
            // Reduced from 50 to 20 for better stability
            var maxConcurrency = 20;
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task<(bool success, CampaignMessage message, string? twilioSid, string? error)>>();
            
            // Check calling hours BEFORE processing any messages
            var campaign = await context.Campaigns.FindAsync(campaignId);
            if (campaign != null && campaign.EnforceCallingHours && !IsWithinCallingHours(campaign))
            {
                _logger.LogWarning($"Campaign {campaignId} processing skipped - outside calling hours. Will retry later.");
                // Mark messages back to pending so they can be processed later
                foreach (var msg in messages)
                {
                    msg.Status = MessageStatus.Pending;
                }
                await context.SaveChangesAsync();
                return;
            }
            
            foreach (var message in messages)
            {
                // Check if we're outside calling hours before queuing more calls
                if (tasks.Count % 50 == 0 && tasks.Count > 0)
                {
                    campaign = await context.Campaigns.FindAsync(campaignId);
                    if (campaign != null && campaign.EnforceCallingHours && !IsWithinCallingHours(campaign))
                    {
                        _logger.LogWarning($"Campaign {campaignId} paused - outside calling hours. Queued {tasks.Count} calls.");
                        break;
                    }
                }
                
                tasks.Add(ProcessSingleRobocallBatchAsync(message, voiceUrl, twilioService, semaphore));
            }
            
            // Wait for all calls to complete
            var results = await Task.WhenAll(tasks);
            
            // Process results and update in memory
            foreach (var (success, message, twilioSid, error) in results)
            {
                if (success)
                {
                    successes++;
                    message.Status = MessageStatus.Sent;
                    message.SentAt = DateTime.UtcNow;
                    message.TwilioSid = twilioSid;
                    
                    if (message.Voter != null)
                    {
                        UpdateVoterCampaignStats(message.Voter, campaignId, CampaignType.RoboCall);
                    }
                }
                else
                {
                    failures++;
                    message.Status = MessageStatus.Failed;
                    message.ErrorMessage = error ?? "Unknown error";
                    message.FailedAt = DateTime.UtcNow;
                }
            }
            
            // Single batch save for all changes
            _logger.LogInformation($"Saving {messages.Count} message updates to database in single batch");
            await context.SaveChangesAsync();
            
            // Update campaign final status
            await UpdateCampaignFinalStatusAsync(campaignId, successes, failures);
            
            _logger.LogInformation($"Robocall campaign {campaignId} completed: {successes} successful, {failures} failed");
        }
        
        private async Task<(bool success, CampaignMessage message, string? twilioSid, string? error)> ProcessSingleRobocallBatchAsync(
            CampaignMessage message,
            string voiceUrl,
            ITwilioService twilioService,
            SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                // Increased delay to 50ms to reduce load (20 calls per second max)
                await Task.Delay(50);
                
                var (success, twilioSid, error) = await twilioService.MakeRoboCallWithoutDbUpdateAsync(
                    message.RecipientPhone,
                    voiceUrl);
                    
                return (success, message, twilioSid, error);
            }
            catch (Exception ex)
            {
                return (false, message, null, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        
        private async Task ProcessSmsCampaignAsync(int campaignId, List<int> messageIds, string campaignMessage, bool overrideOptIn)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var twilioService = scope.ServiceProvider.GetRequiredService<ITwilioService>();
            
            // Load all messages
            var messages = await context.CampaignMessages
                .Include(m => m.Voter)
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();
                
            // Prepare SMS batch
            var smsMessages = messages
                .Select(m => (m.RecipientPhone, campaignMessage, m.Id))
                .ToList();

            // Use new batch method that doesn't update DB individually
            var results = await twilioService.SendBulkSmsWithBatchUpdateAsync(smsMessages, overrideOptIn);
            
            var successes = 0;
            var failures = 0;
            
            // Create lookup dictionary for faster access
            var messageDict = messages.ToDictionary(m => m.Id);
            
            // Process results and update in memory
            foreach (var (success, campaignMessageId, twilioSid, error) in results)
            {
                if (messageDict.TryGetValue(campaignMessageId, out var message))
                {
                    if (success)
                    {
                        successes++;
                        message.Status = MessageStatus.Sent;
                        message.SentAt = DateTime.UtcNow;
                        message.TwilioSid = twilioSid;
                        
                        // Update voter stats
                        if (message.Voter != null)
                        {
                            UpdateVoterCampaignStats(message.Voter, campaignId, CampaignType.SMS);
                        }
                    }
                    else
                    {
                        failures++;
                        message.Status = MessageStatus.Failed;
                        message.ErrorMessage = error ?? "Unknown error";
                        message.FailedAt = DateTime.UtcNow;
                    }
                }
            }
            
            // Single batch save for all changes
            _logger.LogInformation($"Saving {messages.Count} SMS updates to database in single batch");
            await context.SaveChangesAsync();
            
            // Update campaign final status
            await UpdateCampaignFinalStatusAsync(campaignId, successes, failures);
        }
        
        private void UpdateVoterCampaignStats(Voter voter, int campaignId, CampaignType campaignType)
        {
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
        }
        
        private async Task UpdateCampaignProgressAsync(int campaignId, int additionalSuccesses, int additionalFailures)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var campaign = await context.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.SuccessfulDeliveries += additionalSuccesses;
                campaign.FailedDeliveries += additionalFailures;
                campaign.PendingDeliveries = Math.Max(0, campaign.TotalRecipients - campaign.SuccessfulDeliveries - campaign.FailedDeliveries);
                
                await context.SaveChangesAsync();
            }
        }
        
        private async Task UpdateCampaignFinalStatusAsync(int campaignId, int totalSuccesses, int totalFailures)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var campaign = await context.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Completed;
                campaign.SuccessfulDeliveries = totalSuccesses;
                campaign.FailedDeliveries = totalFailures;
                campaign.PendingDeliveries = 0;
                campaign.SentAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync();
            }
        }
        
        private async Task MarkCampaignAsFailedAsync(int campaignId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var campaign = await context.Campaigns.FindAsync(campaignId);
            if (campaign != null)
            {
                campaign.Status = CampaignStatus.Failed;
                await context.SaveChangesAsync();
            }
        }
        
        private bool IsWithinCallingHours(Campaign campaign)
        {
            if (!campaign.EnforceCallingHours)
                return true;

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_callingHoursSettings.TimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                
                // Check if weekend and weekends are not allowed
                if (!campaign.IncludeWeekends && 
                    (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday))
                {
                    _logger.LogDebug($"Weekend calling not allowed for campaign {campaign.Id} - current day: {localTime.DayOfWeek}");
                    return false;
                }
                
                // Check if within allowed hours
                var currentHour = localTime.Hour;
                var isWithinHours = currentHour >= campaign.StartHour && 
                                   currentHour < campaign.EndHour;
                
                if (!isWithinHours)
                {
                    _logger.LogDebug($"Outside calling hours for campaign {campaign.Id} - current: {currentHour}, allowed: {campaign.StartHour}-{campaign.EndHour}");
                }
                
                return isWithinHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking calling hours, defaulting to allowed");
                return true;
            }
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

                // Update retry count for failed messages first
                foreach (var msg in failedMessages)
                {
                    msg.RetryCount++;
                    msg.Status = MessageStatus.Queued;
                    msg.ErrorMessage = null;
                }
                await _context.SaveChangesAsync();
                
                // Start retry processing in background to avoid HTTP timeouts
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation($"Starting background retry for campaign {campaignId}");
                        
                        if (campaign.Type == CampaignType.SMS)
                        {
                            var messageList = messagesToRetry.Select(m => 
                                (m.PhoneNumber, m.Message, m.CampaignMessageId)).ToList();
                            
                            await _twilioService.SendBulkSmsAsync(messageList, overrideOptIn);
                        }
                        else if (campaign.Type == CampaignType.RoboCall)
                        {
                            // Retry robo calls concurrently
                            _logger.LogInformation($"Retrying {messagesToRetry.Count} robocalls with high concurrency");
                            
                            var maxConcurrency = 50; // 50 concurrent calls
                            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                            var tasks = messagesToRetry
                                .Where(msg => !string.IsNullOrEmpty(msg.VoiceUrl))
                                .Select(async msg =>
                                {
                                    await semaphore.WaitAsync();
                                    try
                                    {
                                        await Task.Delay(20); // 20ms delay = up to 50 calls/second
                                        await _twilioService.MakeRoboCallAsync(msg.PhoneNumber, msg.VoiceUrl, msg.CampaignMessageId);
                                    }
                                    finally
                                    {
                                        semaphore.Release();
                                    }
                                });
                                
                            await Task.WhenAll(tasks);
                        }
                        
                        _logger.LogInformation($"Background retry completed for campaign {campaignId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error during background retry for campaign {campaignId}");
                    }
                });

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

        public async Task<List<Campaign>> CheckAndResumeStuckCampaignsAsync()
        {
            var resumedCampaigns = new List<Campaign>();
            
            try
            {
                // Find campaigns that are stuck in "Sending" status with pending messages
                var stuckCampaigns = await _context.Campaigns
                    .Where(c => c.Status == CampaignStatus.Sending && c.PendingDeliveries > 0)
                    .ToListAsync();
                
                foreach (var campaign in stuckCampaigns)
                {
                    _logger.LogInformation($"Found stuck campaign {campaign.Id} ({campaign.Name}) with {campaign.PendingDeliveries} pending messages");
                    
                    // Check if there's already an active operation for this campaign
                    var operationName = $"Campaign_{campaign.Id}_Processing";
                    var activeOps = _backgroundMonitor.GetActiveOperations();
                    
                    if (!activeOps.ContainsKey(operationName))
                    {
                        _logger.LogInformation($"Resuming campaign {campaign.Id} ({campaign.Name})");
                        
                        // Resume the campaign by directly calling the processing method
                        _ = Task.Run(async () => 
                        {
                            _logger.LogInformation($"Background task resumed for campaign {campaign.Id}");
                            await ProcessCampaignMessagesWithScopeAsync(campaign.Id, true);
                        });
                        
                        resumedCampaigns.Add(campaign);
                    }
                    else
                    {
                        _logger.LogInformation($"Campaign {campaign.Id} already has an active processing operation");
                    }
                }
                
                if (resumedCampaigns.Any())
                {
                    _logger.LogInformation($"Resumed {resumedCampaigns.Count} stuck campaigns");
                }
                else if (stuckCampaigns.Any())
                {
                    _logger.LogInformation($"Found {stuckCampaigns.Count} stuck campaigns but all have active operations");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and resuming stuck campaigns");
            }
            
            return resumedCampaigns;
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

        public async Task<Campaign?> DuplicateCampaignAsync(int campaignId, string userId)
        {
            try
            {
                var originalCampaign = await _context.Campaigns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == campaignId);

                if (originalCampaign == null)
                    return null;

                // Create a copy of the campaign
                var duplicatedCampaign = new Campaign
                {
                    Name = $"{originalCampaign.Name} (Copy)",
                    Message = originalCampaign.Message,
                    Type = originalCampaign.Type,
                    Status = CampaignStatus.Draft,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    VoiceUrl = originalCampaign.VoiceUrl,
                    RecordingUrl = originalCampaign.RecordingUrl,
                    VoiceRecordingId = originalCampaign.VoiceRecordingId,
                    FilterZipCodes = originalCampaign.FilterZipCodes,
                    FilterVoteFrequency = originalCampaign.FilterVoteFrequency,
                    FilterMinAge = originalCampaign.FilterMinAge,
                    FilterMaxAge = originalCampaign.FilterMaxAge,
                    FilterVoterSupport = originalCampaign.FilterVoterSupport,
                    FilterTags = originalCampaign.FilterTags,
                    TotalRecipients = 0,
                    SuccessfulDeliveries = 0,
                    FailedDeliveries = 0,
                    PendingDeliveries = 0
                };

                _context.Campaigns.Add(duplicatedCampaign);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign {campaignId} duplicated as campaign {duplicatedCampaign.Id} by user {userId}");

                return duplicatedCampaign;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error duplicating campaign {campaignId}");
                return null;
            }
        }
    }
}