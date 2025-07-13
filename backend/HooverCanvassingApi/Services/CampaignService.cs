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

        public async Task<bool> SendCampaignAsync(int campaignId, bool overrideOptIn = false, int? batchSize = null, int? batchDelayMinutes = null)
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
            _logger.LogInformation($"Starting background task for campaign {campaignId} with overrideOptIn={overrideOptIn}, batchSize={batchSize}, batchDelayMinutes={batchDelayMinutes}");
            _ = Task.Run(async () => 
            {
                _logger.LogInformation($"Background task started for campaign {campaignId}");
                await ProcessCampaignMessagesWithScopeAsync(campaignId, overrideOptIn, batchSize, batchDelayMinutes);
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

        private async Task ProcessCampaignMessagesWithScopeAsync(int campaignId, bool overrideOptIn = false, int? batchSize = null, int? batchDelayMinutes = null)
        {
            var operationName = $"Campaign_{campaignId}_Processing";
            _logger.LogInformation($"ProcessCampaignMessagesWithScopeAsync called for campaign {campaignId} with overrideOptIn={overrideOptIn}");
            
            // Track start time to handle 30-minute limit
            var startTime = DateTime.UtcNow;
            var maxRuntime = TimeSpan.FromMinutes(25); // Stop at 25 minutes to be safe
            
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
                var successfulDeliveries = 0;
                var failedDeliveries = 0;

                if (campaignType == CampaignType.SMS)
                {
                    await ProcessSmsCampaignAsync(campaignId, pendingMessageIds, campaignMessage!, overrideOptIn);
                }
                else
                {
                    // Process robo calls with batching
                    var effectiveBatchSize = batchSize ?? pendingMessageIds.Count;
                    var effectiveDelayMinutes = batchDelayMinutes ?? 0;
                    
                    _logger.LogInformation($"Processing {totalMessages} robocalls in batches of {effectiveBatchSize} with {effectiveDelayMinutes} minute delays");
                    
                    for (int batchIndex = 0; batchIndex < pendingMessageIds.Count; batchIndex += effectiveBatchSize)
                    {
                        // Check if we're outside calling hours (for robocalls only)
                        if (campaignType == CampaignType.RoboCall)
                        {
                            // Get current campaign settings
                            Campaign? currentCampaign = null;
                            using (var checkScope = _serviceScopeFactory.CreateScope())
                            {
                                var checkContext = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                                currentCampaign = await checkContext.Campaigns.FindAsync(campaignId);
                            }
                            
                            if (currentCampaign != null && currentCampaign.EnforceCallingHours && !IsWithinCallingHours(currentCampaign))
                            {
                                _logger.LogWarning($"Campaign {campaignId} paused - outside calling hours. Processed {successfulDeliveries} calls so far.");
                                
                                // Update campaign status to allow resumption
                                await UpdateCampaignProgressAsync(campaignId, successfulDeliveries, failedDeliveries);
                                
                                _backgroundMonitor.CompleteOperation(operationName);
                                _logger.LogInformation($"Campaign {campaignId} will automatically resume during calling hours");
                                return;
                            }
                        }
                        
                        // Check if we're approaching the 30-minute limit
                        var elapsed = DateTime.UtcNow - startTime;
                        if (elapsed > maxRuntime)
                        {
                            _logger.LogWarning($"Campaign {campaignId} processing approaching 30-minute limit after {elapsed.TotalMinutes:F1} minutes. Processed {batchIndex} messages so far.");
                            
                            // Update campaign status to allow resumption
                            await UpdateCampaignProgressAsync(campaignId, successfulDeliveries, failedDeliveries);
                            
                            _backgroundMonitor.CompleteOperation(operationName);
                            _logger.LogInformation($"Campaign {campaignId} paused for resumption. Will continue from message index {batchIndex}");
                            return;
                        }
                        
                        // If this is not the first batch and we have a delay, wait
                        if (batchIndex > 0 && effectiveDelayMinutes > 0)
                        {
                            _logger.LogInformation($"Waiting {effectiveDelayMinutes} minutes before processing next batch...");
                            await Task.Delay(TimeSpan.FromMinutes(effectiveDelayMinutes));
                        }
                        
                        var batchMessageIds = pendingMessageIds
                            .Skip(batchIndex)
                            .Take(effectiveBatchSize)
                            .ToList();
                            
                        var batchNumber = (batchIndex / effectiveBatchSize) + 1;
                        _logger.LogInformation($"Processing batch {batchNumber} with {batchMessageIds.Count} calls");
                        
                        // Process batch with its own database context
                        var (batchSuccesses, batchFailures) = await ProcessRobocallBatchAsync(
                            campaignId, batchMessageIds, voiceUrl!, overrideOptIn, campaignType);
                            
                        successfulDeliveries += batchSuccesses;
                        failedDeliveries += batchFailures;
                        
                        _logger.LogInformation($"Batch {batchNumber} completed. Total progress: {successfulDeliveries} successful, {failedDeliveries} failed");
                    }
                }

                // Final update of campaign status
                await UpdateCampaignFinalStatusAsync(campaignId, successfulDeliveries, failedDeliveries);
                
                _backgroundMonitor.CompleteOperation(operationName);
                _logger.LogInformation($"Campaign {campaignId} completed: {successfulDeliveries} successful, {failedDeliveries} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing campaign {campaignId}");
                _backgroundMonitor.FailOperation(operationName, ex.Message);
                
                // Mark campaign as failed
                await MarkCampaignAsFailedAsync(campaignId);
            }
        }
        
        private async Task<(int successes, int failures)> ProcessRobocallBatchAsync(
            int campaignId, List<int> messageIds, string voiceUrl, bool overrideOptIn, CampaignType campaignType)
        {
            var successes = 0;
            var failures = 0;
            
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var twilioService = scope.ServiceProvider.GetRequiredService<ITwilioService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<CampaignService>>();
            
            // Load messages for this batch
            var messages = await context.CampaignMessages
                .Include(m => m.Voter)
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();
                
            // Get phone number pool service
            var phoneNumberPool = scope.ServiceProvider.GetRequiredService<IPhoneNumberPoolService>();
            
            // Check if we have phone numbers in the pool
            var availableNumbers = await phoneNumberPool.GetAllNumbersAsync();
            var activeNumberCount = availableNumbers.Count(n => n.IsActive);
            
            if (activeNumberCount == 0)
            {
                // Fall back to sequential processing with default number
                logger.LogWarning("No phone numbers in pool, falling back to sequential processing");
                
                foreach (var message in messages)
                {
                    try
                    {
                        await Task.Delay(1000); // Rate limiting
                        var success = await twilioService.MakeRoboCallAsync(
                            message.RecipientPhone, 
                            voiceUrl, 
                            message.Id);
                        if (success)
                        {
                            successes++;
                            message.Status = MessageStatus.Sent;
                            message.SentAt = DateTime.UtcNow;
                            
                            if (message.Voter != null)
                            {
                                UpdateVoterCampaignStats(message.Voter, campaignId, campaignType);
                            }
                        }
                        else
                        {
                            failures++;
                            message.Status = MessageStatus.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing message {message.Id}");
                        failures++;
                        message.Status = MessageStatus.Failed;
                        message.ErrorMessage = ex.Message;
                    }
                }
            }
            else
            {
                // Process calls concurrently using phone number pool
                var maxConcurrency = Math.Min(activeNumberCount * 2, 10); // Allow some oversubscription but cap at 10
                logger.LogInformation($"Processing {messages.Count} calls with {activeNumberCount} phone numbers (max concurrency: {maxConcurrency})");
                
                var semaphore = new SemaphoreSlim(maxConcurrency);
                var tasks = new List<Task<(bool success, CampaignMessage message, Exception? error)>>();
                
                foreach (var message in messages)
                {
                    tasks.Add(ProcessSingleRobocallAsync(message, voiceUrl, twilioService, semaphore, logger));
                }
                
                var results = await Task.WhenAll(tasks);
                
                foreach (var (success, message, error) in results)
                {
                    if (success)
                    {
                        successes++;
                        message.Status = MessageStatus.Sent;
                        message.SentAt = DateTime.UtcNow;
                        
                        if (message.Voter != null)
                        {
                            UpdateVoterCampaignStats(message.Voter, campaignId, campaignType);
                        }
                    }
                    else
                    {
                        failures++;
                        message.Status = MessageStatus.Failed;
                        if (error != null)
                        {
                            message.ErrorMessage = error.Message;
                        }
                    }
                }
            }
            
            // Save all changes for this batch
            await context.SaveChangesAsync();
            
            // Update campaign progress
            await UpdateCampaignProgressAsync(campaignId, successes, failures);
            
            return (successes, failures);
        }
        
        private async Task<(bool success, CampaignMessage message, Exception? error)> ProcessSingleRobocallAsync(
            CampaignMessage message, 
            string voiceUrl, 
            ITwilioService twilioService, 
            SemaphoreSlim semaphore,
            ILogger<CampaignService> logger)
        {
            await semaphore.WaitAsync();
            try
            {
                // Small random delay to avoid thundering herd
                await Task.Delay(Random.Shared.Next(100, 500));
                
                var success = await twilioService.MakeRoboCallAsync(
                    message.RecipientPhone, 
                    voiceUrl, 
                    message.Id);
                    
                return (success, message, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing message {message.Id}");
                return (false, message, ex);
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

            var results = await twilioService.SendBulkSmsAsync(smsMessages, overrideOptIn);
            
            var successes = 0;
            var failures = 0;
            
            // Process results and update voter stats
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i])
                {
                    successes++;
                    messages[i].Status = MessageStatus.Sent;
                    messages[i].SentAt = DateTime.UtcNow;
                    
                    // Update voter stats
                    if (messages[i].Voter != null)
                    {
                        UpdateVoterCampaignStats(messages[i].Voter, campaignId, CampaignType.SMS);
                    }
                }
                else
                {
                    failures++;
                    messages[i].Status = MessageStatus.Failed;
                }
            }
            
            await context.SaveChangesAsync();
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


        public async Task<bool> RetryFailedMessagesAsync(int campaignId, bool overrideOptIn = false, int? batchSize = null, int? batchDelayMinutes = null)
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
                    // Retry robo calls with optional batching
                    var effectiveBatchSize = batchSize ?? messagesToRetry.Count;
                    var effectiveDelayMinutes = batchDelayMinutes ?? 0;
                    
                    _logger.LogInformation($"Retrying {messagesToRetry.Count} robocalls in batches of {effectiveBatchSize} with {effectiveDelayMinutes} minute delays");
                    
                    for (int batchIndex = 0; batchIndex < messagesToRetry.Count; batchIndex += effectiveBatchSize)
                    {
                        if (batchIndex > 0 && effectiveDelayMinutes > 0)
                        {
                            _logger.LogInformation($"Waiting {effectiveDelayMinutes} minutes before retrying next batch...");
                            await Task.Delay(TimeSpan.FromMinutes(effectiveDelayMinutes));
                        }
                        
                        var batch = messagesToRetry.Skip(batchIndex).Take(effectiveBatchSize).ToList();
                        _logger.LogInformation($"Retrying batch {(batchIndex / effectiveBatchSize) + 1} with {batch.Count} calls");
                        
                        foreach (var msg in batch)
                        {
                            if (!string.IsNullOrEmpty(msg.VoiceUrl))
                            {
                                await Task.Delay(1000); // Small delay between calls
                                await _twilioService.MakeRoboCallAsync(msg.PhoneNumber, msg.VoiceUrl, msg.CampaignMessageId);
                            }
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
                            await ProcessCampaignMessagesWithScopeAsync(campaign.Id, true, 50, 5);
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