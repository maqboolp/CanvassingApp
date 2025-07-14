using HooverCanvassingApi.Data;
using HooverCanvassingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HooverCanvassingApi.Services
{
    public interface IPhoneNumberPoolInitializer
    {
        Task InitializeAsync();
    }

    public class PhoneNumberPoolInitializer : IHostedService, IPhoneNumberPoolInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhoneNumberPoolInitializer> _logger;

        public PhoneNumberPoolInitializer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<PhoneNumberPoolInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // Get the main Twilio phone number from configuration
                string? mainPhoneNumber = null;
                
                // Try to load from database configuration first
                var dbSettings = await context.TwilioConfigurations
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();
                
                if (dbSettings != null)
                {
                    mainPhoneNumber = dbSettings.FromPhoneNumber;
                }
                else
                {
                    mainPhoneNumber = _configuration["Twilio:FromPhoneNumber"];
                }

                if (string.IsNullOrEmpty(mainPhoneNumber))
                {
                    _logger.LogWarning("No main Twilio phone number configured");
                    return;
                }

                // Check if main number already exists in the pool
                var exists = await context.TwilioPhoneNumbers
                    .AnyAsync(n => n.Number == mainPhoneNumber);

                if (!exists)
                {
                    // Add the main number to the pool
                    var twilioNumber = new TwilioPhoneNumber
                    {
                        Number = mainPhoneNumber,
                        IsActive = true,
                        IsMainNumber = true,
                        MaxConcurrentCalls = 50, // Default Twilio limit
                        CreatedAt = DateTime.UtcNow,
                        Notes = "Main Twilio account phone number"
                    };

                    context.TwilioPhoneNumbers.Add(twilioNumber);
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Added main Twilio phone number {mainPhoneNumber} to pool");
                }
                else
                {
                    // Update the existing number to mark it as main
                    var existingNumber = await context.TwilioPhoneNumbers
                        .FirstOrDefaultAsync(n => n.Number == mainPhoneNumber);
                    
                    if (existingNumber != null && !existingNumber.IsMainNumber)
                    {
                        existingNumber.IsMainNumber = true;
                        await context.SaveChangesAsync();
                        _logger.LogInformation($"Marked existing phone number {mainPhoneNumber} as main number");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing phone number pool");
            }
        }
    }
}