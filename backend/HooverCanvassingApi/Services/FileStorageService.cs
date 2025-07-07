using System.IO;

namespace HooverCanvassingApi.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadAudioAsync(Stream audioStream, string fileName);
        Task<bool> DeleteAudioAsync(string fileUrl);
        Task<string> UploadPhotoAsync(Stream photoStream, string fileName);
        Task<bool> DeletePhotoAsync(string fileUrl);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _uploadPath;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Default to wwwroot/uploads/audio
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "audio");
            
            // Ensure directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
        {
            try
            {
                // Sanitize filename - replace spaces and special characters
                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    fileName, 
                    @"[^\w\-_.]+", 
                    "_"
                );
                
                // Generate unique filename with sanitized name
                var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}_{sanitizedFileName}";
                var filePath = Path.Combine(_uploadPath, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await audioStream.CopyToAsync(fileStream);
                }

                // Return absolute URL for Twilio compatibility
                var baseUrl = _configuration["Backend:BaseUrl"] ?? 
                              _configuration["Frontend:BaseUrl"] ?? 
                              $"{_configuration["JwtSettings:Issuer"]}" ??
                              "http://localhost:5131";
                
                // Ensure baseUrl doesn't have trailing slash
                baseUrl = baseUrl.TrimEnd('/');
                
                var absoluteUrl = $"{baseUrl}/uploads/audio/{uniqueFileName}";
                
                _logger.LogInformation("Audio file uploaded successfully: {FileName}, URL: {Url}", 
                    uniqueFileName, absoluteUrl);
                
                return absoluteUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio file");
                throw;
            }
        }

        public async Task<bool> DeleteAudioAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // Extract filename from URL (handle both relative and absolute URLs)
                var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
                var fileName = Path.GetFileName(uri.IsAbsoluteUri ? uri.LocalPath : fileUrl);
                var filePath = Path.Combine(_uploadPath, fileName);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Audio file deleted successfully: {FileName}", fileName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file: {FileUrl}", fileUrl);
                return false;
            }
        }

        public async Task<string> UploadPhotoAsync(Stream photoStream, string fileName)
        {
            try
            {
                // Create photos directory if it doesn't exist
                var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
                if (!Directory.Exists(photoPath))
                {
                    Directory.CreateDirectory(photoPath);
                }

                // Sanitize filename - replace spaces and special characters
                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    fileName, 
                    @"[^\w\-_.]+", 
                    "_"
                );
                
                // Generate unique filename with sanitized name
                var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}_{sanitizedFileName}";
                var filePath = Path.Combine(photoPath, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photoStream.CopyToAsync(fileStream);
                }

                // Return absolute URL for consistency
                var baseUrl = _configuration["Backend:BaseUrl"] ?? 
                              _configuration["Frontend:BaseUrl"] ?? 
                              $"{_configuration["JwtSettings:Issuer"]}" ??
                              "http://localhost:5131";
                
                // Ensure baseUrl doesn't have trailing slash
                baseUrl = baseUrl.TrimEnd('/');
                
                var absoluteUrl = $"{baseUrl}/uploads/photos/{uniqueFileName}";
                
                _logger.LogInformation("Photo file uploaded successfully: {FileName}, URL: {Url}", 
                    uniqueFileName, absoluteUrl);
                    
                return absoluteUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo file");
                throw;
            }
        }

        public async Task<bool> DeletePhotoAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // Extract filename from URL (handle both relative and absolute URLs)
                var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
                var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
                var fileName = Path.GetFileName(uri.IsAbsoluteUri ? uri.LocalPath : fileUrl);
                var filePath = Path.Combine(photoPath, fileName);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Photo file deleted successfully: {FileName}", fileName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo file: {FileUrl}", fileUrl);
                return false;
            }
        }
    }
}