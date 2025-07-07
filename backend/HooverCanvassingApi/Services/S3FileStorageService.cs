using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace HooverCanvassingApi.Services
{
    public class S3FileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3FileStorageService> _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _audioPrefix;
        private readonly string _photoPrefix;
        private readonly string _publicUrl;

        public S3FileStorageService(IConfiguration configuration, ILogger<S3FileStorageService> logger, IAmazonS3 s3Client)
        {
            _configuration = configuration;
            _logger = logger;
            _s3Client = s3Client;
            
            var awsConfig = _configuration.GetSection("AWS:S3");
            _bucketName = awsConfig["BucketName"] ?? "hoover-canvassing-audio";
            _audioPrefix = awsConfig["AudioPrefix"] ?? "audio-memos/";
            _photoPrefix = awsConfig["PhotoPrefix"] ?? "photos/";
            
            var serviceUrl = awsConfig["ServiceUrl"]; // For DigitalOcean Spaces or other S3-compatible services
            var publicUrl = awsConfig["PublicUrl"]; // Custom public URL for DigitalOcean Spaces
            
            _logger.LogInformation("Using S3-compatible service with bucket: {BucketName}", _bucketName);
            
            // Set public URL for DigitalOcean Spaces
            _publicUrl = publicUrl ?? (serviceUrl != null ? serviceUrl.Replace("https://", $"https://{_bucketName}.") : "");
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
                
                // Generate unique key with sanitized filename
                var key = $"{_audioPrefix}{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}_{sanitizedFileName}";
                
                // Determine content type from file extension
                var contentType = Path.GetExtension(fileName).ToLower() switch
                {
                    ".mp4" => "audio/mp4",
                    ".m4a" => "audio/m4a",
                    ".aac" => "audio/aac",
                    ".wav" => "audio/wav",
                    ".mp3" => "audio/mpeg",
                    ".ogg" => "audio/ogg",
                    _ => "audio/webm"
                };
                
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = audioStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    CannedACL = S3CannedACL.PublicRead // Make files publicly readable
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Return the public URL
                    var url = $"{_publicUrl}/{key}";
                    _logger.LogInformation("Audio file uploaded to S3 successfully: {Key}", key);
                    return url;
                }
                
                throw new Exception($"Failed to upload to S3. Status: {response.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio file to S3");
                throw;
            }
        }

        public async Task<bool> DeleteAudioAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;
                
                // Extract key from URL
                var uri = new Uri(fileUrl);
                var key = uri.AbsolutePath.TrimStart('/');
                
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };
                
                var response = await _s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation("Audio file deleted from S3 successfully: {Key}", key);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio file from S3: {FileUrl}", fileUrl);
                return false;
            }
        }

        public async Task<string> UploadPhotoAsync(Stream photoStream, string fileName)
        {
            try
            {
                // Sanitize filename - replace spaces and special characters
                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    fileName, 
                    @"[^\w\-_.]+", 
                    "_"
                );
                
                // Generate unique key with sanitized filename
                var key = $"{_photoPrefix}{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}_{sanitizedFileName}";
                
                // Determine content type from file extension
                var contentType = Path.GetExtension(fileName).ToLower() switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "image/jpeg"
                };
                
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = photoStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    CannedACL = S3CannedACL.PublicRead // Make files publicly readable
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Return the public URL
                    var url = $"{_publicUrl}/{key}";
                    _logger.LogInformation("Photo file uploaded to S3 successfully: {Key}", key);
                    return url;
                }
                
                throw new Exception($"Failed to upload photo to S3. Status: {response.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo file to S3");
                throw;
            }
        }

        public async Task<bool> DeletePhotoAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;
                
                // Extract key from URL
                var uri = new Uri(fileUrl);
                var key = uri.AbsolutePath.TrimStart('/');
                
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };
                
                var response = await _s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation("Photo file deleted from S3 successfully: {Key}", key);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo file from S3: {FileUrl}", fileUrl);
                return false;
            }
        }
        
        // Create bucket if it doesn't exist
        public async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
                
                if (!bucketExists)
                {
                    _logger.LogInformation("Creating S3 bucket: {BucketName}", _bucketName);
                    
                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _bucketName
                    });
                    
                    // Set bucket policy to allow public read access for audio and photo files
                    var bucketPolicy = @"{
                        ""Version"": ""2012-10-17"",
                        ""Statement"": [
                            {
                                ""Sid"": ""PublicReadGetObject"",
                                ""Effect"": ""Allow"",
                                ""Principal"": ""*"",
                                ""Action"": ""s3:GetObject"",
                                ""Resource"": [
                                    ""arn:aws:s3:::" + _bucketName + @"/" + _audioPrefix + @"*"",
                                    ""arn:aws:s3:::" + _bucketName + @"/" + _photoPrefix + @"*""
                                ]
                            }
                        ]
                    }";
                    
                    await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                    {
                        BucketName = _bucketName,
                        Policy = bucketPolicy
                    });
                    
                    _logger.LogInformation("S3 bucket created successfully: {BucketName}", _bucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring S3 bucket exists");
                throw;
            }
        }
    }
}