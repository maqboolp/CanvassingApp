using HooverCanvassingApi.Configuration;
using HooverCanvassingApi.Models;
using Microsoft.Extensions.Options;

namespace HooverCanvassingApi.Services
{
    public class ConfigurationValidationService : IHostedService
    {
        private readonly ILogger<ConfigurationValidationService> _logger;
        private readonly CampaignSettings _campaignSettings;
        private readonly EmailSettings _emailSettings;

        public ConfigurationValidationService(
            ILogger<ConfigurationValidationService> logger,
            IOptions<CampaignSettings> campaignSettings,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _campaignSettings = campaignSettings.Value;
            _emailSettings = emailSettings.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== Configuration Validation Starting ===");
            
            var missingConfigs = new List<string>();

            // Check Campaign Settings
            CheckConfig("Campaign.CandidateName", _campaignSettings.CandidateName, missingConfigs);
            CheckConfig("Campaign.CampaignName", _campaignSettings.CampaignName, missingConfigs);
            CheckConfig("Campaign.CampaignTitle", _campaignSettings.CampaignTitle, missingConfigs);
            CheckConfig("Campaign.PaidForBy", _campaignSettings.PaidForBy, missingConfigs);
            CheckConfig("Campaign.CampaignEmail", _campaignSettings.CampaignEmail, missingConfigs);
            CheckConfig("Campaign.CampaignPhone", _campaignSettings.CampaignPhone, missingConfigs);
            CheckConfig("Campaign.OptInConsentText", _campaignSettings.OptInConsentText, missingConfigs);
            CheckConfig("Campaign.Office", _campaignSettings.Office, missingConfigs);
            CheckConfig("Campaign.Jurisdiction", _campaignSettings.Jurisdiction, missingConfigs);

            // Check Email Settings
            if (_emailSettings.Provider == "SendGrid")
            {
                CheckConfig("EmailSettings.SendGridApiKey", _emailSettings.SendGridApiKey, missingConfigs);
            }
            CheckConfig("EmailSettings.FromEmail", _emailSettings.FromEmail, missingConfigs);
            CheckConfig("EmailSettings.FromName", _emailSettings.FromName, missingConfigs);

            if (missingConfigs.Any())
            {
                _logger.LogWarning("=== MISSING CONFIGURATION DETECTED ===");
                _logger.LogWarning("The following environment variables are not configured:");
                foreach (var config in missingConfigs)
                {
                    _logger.LogWarning("  - {ConfigName}", config);
                }
                _logger.LogWarning("Please set these environment variables for proper operation.");
                _logger.LogWarning("See ENVIRONMENT_VARIABLES.md for documentation.");
            }
            else
            {
                _logger.LogInformation("All required configuration values are set.");
            }

            _logger.LogInformation("=== Configuration Validation Complete ===");
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void CheckConfig(string configName, string value, List<string> missingConfigs)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("[") && value.EndsWith("]"))
            {
                missingConfigs.Add(configName);
            }
        }
    }
}