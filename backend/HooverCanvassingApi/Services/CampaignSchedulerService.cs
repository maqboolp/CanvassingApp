using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HooverCanvassingApi.Services
{
    public class CampaignSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignSchedulerService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public CampaignSchedulerService(IServiceProvider serviceProvider, ILogger<CampaignSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Campaign Scheduler Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledCampaigns();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Campaign Scheduler Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Campaign Scheduler Service stopped");
        }

        private async Task ProcessScheduledCampaigns()
        {
            using var scope = _serviceProvider.CreateScope();
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

            try
            {
                _logger.LogDebug("Checking for scheduled campaigns...");
                await campaignService.ProcessScheduledCampaignsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled campaigns");
            }
        }
    }
}