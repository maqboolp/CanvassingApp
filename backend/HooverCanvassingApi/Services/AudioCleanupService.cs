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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            _logger.LogInformation("Starting audio file cleanup process");

            // Find contacts older than retention period with audio files
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            var oldContactsWithAudio = await dbContext.Contacts
                .Where(c => c.Timestamp < cutoffDate && c.AudioFileUrl != null)
                .ToListAsync();

            _logger.LogInformation("Found {Count} old contacts with audio files to clean up", oldContactsWithAudio.Count);

            var deletedCount = 0;
            foreach (var contact in oldContactsWithAudio)
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

            if (deletedCount > 0)
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} old audio files", deletedCount);
            }
        }
    }
}