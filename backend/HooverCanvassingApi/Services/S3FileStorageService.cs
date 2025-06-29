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

        public S3FileStorageService(IConfiguration configuration, ILogger<S3FileStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var awsConfig = _configuration.GetSection("AWS:S3");
            _bucketName = awsConfig["BucketName"] ?? "hoover-canvassing-audio";
            _audioPrefix = awsConfig["AudioPrefix"] ?? "audio-memos/";
            
            var accessKey = awsConfig["AccessKey"];
            var secretKey = awsConfig["SecretKey"];
            var region = _configuration["AWS:Region"] ?? "us-east-1";
            
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
            }
            else
            {
                // Use default credentials (IAM role, environment variables, etc.)
                _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            }
        }

        public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
        {
            try
            {
                // Generate unique key
                var key = $"{_audioPrefix}{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}";
                
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = audioStream,
                    ContentType = "audio/webm",
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    CannedACL = S3CannedACL.PublicRead // Make files publicly readable
                };

                var response = await _s3Client.PutObjectAsync(request);
                
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Return the public URL
                    var url = $"https://{_bucketName}.s3.amazonaws.com/{key}";
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
                    
                    // Set bucket policy to allow public read access for audio files
                    var bucketPolicy = @"{
                        ""Version"": ""2012-10-17"",
                        ""Statement"": [
                            {
                                ""Sid"": ""PublicReadGetObject"",
                                ""Effect"": ""Allow"",
                                ""Principal"": ""*"",
                                ""Action"": ""s3:GetObject"",
                                ""Resource"": ""arn:aws:s3:::" + _bucketName + @"/" + _audioPrefix + @"*""
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