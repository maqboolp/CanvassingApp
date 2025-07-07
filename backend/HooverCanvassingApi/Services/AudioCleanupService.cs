using Microsoft.EntityFrameworkCore;
using HooverCanvassingApi.Data;

namespace HooverCanvassingApi.Services
{
    public class AudioCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AudioCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Run daily
        private readonly int _retentionDays = 90; // Keep audio files for 90 days

        public AudioCleanupService(IServiceProvider serviceProvider, ILogger<AudioCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldAudioFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during audio cleanup");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CleanupOldAudioFiles()
        {
            _logger.LogInformation("Starting audio file cleanup process");

            // First, get the list of contact IDs to clean up
            List<string> contactIdsToClean;
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                contactIdsToClean = await dbContext.Contacts
                    .Where(c => c.Timestamp < cutoffDate && c.AudioFileUrl != null)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            _logger.LogInformation("Found {Count} old contacts with audio files to clean up", contactIdsToClean.Count);

            var deletedCount = 0;
            var batchSize = 50; // Process in batches to avoid long-lived connections
            
            for (int i = 0; i < contactIdsToClean.Count; i += batchSize)
            {
                var batchIds = contactIdsToClean.Skip(i).Take(batchSize).ToList();
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                    
                    var contacts = await dbContext.Contacts
                        .Where(c => batchIds.Contains(c.Id))
                        .ToListAsync();
                    
                    foreach (var contact in contacts)
                    {
                        try
                        {
                            // Delete the audio file
                            if (!string.IsNullOrEmpty(contact.AudioFileUrl))
                            {
                                var deleted = await fileStorageService.DeleteAudioAsync(contact.AudioFileUrl);
                                if (deleted)
                                {
                                    // Clear the audio fields from the contact
                                    contact.AudioFileUrl = null;
                                    contact.AudioDurationSeconds = null;
                                    deletedCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deleting audio file for contact {ContactId}", contact.Id);
                        }
                    }
                    
                    await dbContext.SaveChangesAsync();
                }
                
                // Small delay between batches to avoid overwhelming the system
                if (i + batchSize < contactIdsToClean.Count)
                {
                    await Task.Delay(100);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old audio files", deletedCount);
        }
    }
}