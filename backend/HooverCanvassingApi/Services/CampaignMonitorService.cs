using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
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
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes for logging only

        public CampaignMonitorService(IServiceProvider serviceProvider, ILogger<CampaignMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Monitor Service started - Manual resumption only mode");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only log stuck campaigns, don't auto-resume
                    await LogStuckCampaigns();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Campaign Monitor Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Campaign Monitor Service stopped");
        }

        private async Task LogStuckCampaigns()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Find campaigns that are stuck in "Sending" status with pending messages
                var stuckCampaigns = await context.Campaigns
                    .Where(c => c.Status == CampaignStatus.Sending && c.PendingDeliveries > 0)
                    .ToListAsync();
                
                if (stuckCampaigns.Any())
                {
                    _logger.LogWarning($"Found {stuckCampaigns.Count} stuck campaigns requiring manual intervention:");
                    foreach (var campaign in stuckCampaigns)
                    {
                        _logger.LogWarning($"Campaign {campaign.Id} ({campaign.Name}) - {campaign.PendingDeliveries} pending messages");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for stuck campaigns");
            }
        }
    }
}