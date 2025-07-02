using FFMpegCore;
using FFMpegCore.Pipes;
using System.IO;

namespace HooverCanvassingApi.Services
{
    public interface IAudioConversionService
    {
        Task<Stream> ConvertWebMToMp3Async(Stream webmStream, string fileName);
        Task<bool> IsFFMpegAvailableAsync();
    }

    public class AudioConversionService : IAudioConversionService
    {
        private readonly ILogger<AudioConversionService> _logger;
        private readonly IConfiguration _configuration;

        public AudioConversionService(ILogger<AudioConversionService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configure FFMpeg options
            var ffmpegPath = _configuration["FFMpeg:BinaryPath"];
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegPath });
            }
        }

        public async Task<bool> IsFFMpegAvailableAsync()
        {
            try
            {
                // Test FFMpeg availability by trying to get the version
                await Task.Run(() => 
                {
                    var probe = GlobalFFOptions.Current.BinaryFolder;
                    _logger.LogInformation($"FFMpeg binary folder: {probe}");
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFMpeg is not available");
                return false;
            }
        }

        public async Task<Stream> ConvertWebMToMp3Async(Stream webmStream, string fileName)
        {
            var outputStream = new MemoryStream();
            
            try
            {
                _logger.LogInformation($"Starting WebM to MP3 conversion for file: {fileName}");
                
                // Create a temporary file for the input stream (FFMpeg works better with files)
                var tempInputPath = Path.GetTempFileName();
                var tempOutputPath = Path.ChangeExtension(tempInputPath, ".mp3");
                
                try
                {
                    // Write input stream to temp file
                    using (var fileStream = File.Create(tempInputPath))
                    {
                        await webmStream.CopyToAsync(fileStream);
                    }
                    
                    // Convert using FFMpeg
                    await FFMpegArguments
                        .FromFileInput(tempInputPath)
                        .OutputToFile(tempOutputPath, true, options => options
                            .WithAudioCodec("libmp3lame")
                            .WithAudioBitrate(128)
                            .WithAudioSamplingRate(44100)
                            .DisableChannel(FFMpegCore.Enums.Channel.Video))
                        .ProcessAsynchronously();
                    
                    // Read converted file to output stream
                    using (var convertedFileStream = File.OpenRead(tempOutputPath))
                    {
                        await convertedFileStream.CopyToAsync(outputStream);
                    }
                    
                    outputStream.Position = 0;
                    _logger.LogInformation($"Successfully converted WebM to MP3 for file: {fileName}. Output size: {outputStream.Length} bytes");
                    
                    return outputStream;
                }
                finally
                {
                    // Clean up temporary files
                    if (File.Exists(tempInputPath))
                        File.Delete(tempInputPath);
                    if (File.Exists(tempOutputPath))
                        File.Delete(tempOutputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to convert WebM to MP3 for file: {fileName}");
                outputStream.Dispose();
                throw new InvalidOperationException($"Audio conversion failed: {ex.Message}", ex);
            }
        }
    }
}