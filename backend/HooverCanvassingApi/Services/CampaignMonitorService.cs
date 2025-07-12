using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HooverCanvassingApi.Services
{
    public class CampaignMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignMonitorService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

        public CampaignMonitorService(IServiceProvider serviceProvider, ILogger<CampaignMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Monitor Service started");

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
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

            try
            {
                _logger.LogDebug("Checking for stuck campaigns...");
                var resumedCampaigns = await campaignService.CheckAndResumeStuckCampaignsAsync();
                
                if (resumedCampaigns.Count > 0)
                {
                    _logger.LogInformation($"Resumed {resumedCampaigns.Count} stuck campaigns");
                    foreach (var campaign in resumedCampaigns)
                    {
                        _logger.LogInformation($"Resumed campaign: {campaign.Id} - {campaign.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and resuming stuck campaigns");
            }
        }
    }
}