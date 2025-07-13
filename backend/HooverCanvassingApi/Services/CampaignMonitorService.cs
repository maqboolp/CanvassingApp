using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using HooverCanvassingApi.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HooverCanvassingApi.Services
{
    public class CampaignMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignMonitorService> _logger;
        private readonly CallingHoursSettings _callingHoursSettings;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

        public CampaignMonitorService(
            IServiceProvider serviceProvider, 
            ILogger<CampaignMonitorService> logger,
            IOptions<CallingHoursSettings> callingHoursSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _callingHoursSettings = callingHoursSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Monitor Service started - Time-based automatic resumption mode");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndResumeStuckCampaigns();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Campaign Monitor Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Campaign Monitor Service stopped");
        }

        private async Task CheckAndResumeStuckCampaigns()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

            try
            {
                // Find campaigns that are stuck in "Sending" status with pending messages
                var stuckCampaigns = await context.Campaigns
                    .Where(c => c.Status == CampaignStatus.Sending && c.PendingDeliveries > 0)
                    .ToListAsync();
                
                if (stuckCampaigns.Any())
                {
                    _logger.LogInformation($"Found {stuckCampaigns.Count} stuck campaigns");
                    
                    // Check each campaign for inactivity
                    foreach (var campaign in stuckCampaigns)
                    {
                        // Check if this specific campaign should respect calling hours
                        if (campaign.Type == CampaignType.RoboCall && campaign.EnforceCallingHours)
                        {
                            // Check if we're within this campaign's calling hours
                            if (!IsWithinCampaignCallingHours(campaign))
                            {
                                _logger.LogDebug($"Campaign {campaign.Id} is outside its calling hours - skipping");
                                continue;
                            }
                        }
                        
                        var lastActivity = await context.CampaignMessages
                            .Where(m => m.CampaignId == campaign.Id && m.SentAt != null)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.SentAt)
                            .FirstOrDefaultAsync();
                        
                        // Auto-resume if inactive for more than 5 minutes
                        if (lastActivity == null || DateTime.UtcNow - lastActivity.Value > TimeSpan.FromMinutes(5))
                        {
                            _logger.LogInformation($"Auto-resuming campaign {campaign.Id} ({campaign.Name}) - " +
                                $"EnforceCallingHours: {campaign.EnforceCallingHours}");
                            
                            // Use the service to resume
                            var resumedCampaigns = await campaignService.CheckAndResumeStuckCampaignsAsync();
                            
                            if (resumedCampaigns.Count > 0)
                            {
                                _logger.LogInformation($"Successfully auto-resumed {resumedCampaigns.Count} campaigns");
                            }
                            
                            break; // Process one at a time to avoid overload
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and resuming stuck campaigns");
            }
        }

        private bool IsWithinCampaignCallingHours(Campaign campaign)
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
                _logger.LogError(ex, $"Error checking calling hours for campaign {campaign.Id}, defaulting to allowed");
                return true;
            }
        }
    }
}