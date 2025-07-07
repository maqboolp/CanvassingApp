# Fix for Robocall Audio Not Playing

## Problem
When sending robocalls using stored MP3 files, recipients receive the call but hear no audio.

## Root Cause
The audio files are stored with relative URLs (e.g., `/uploads/audio/file.mp3`), but Twilio needs absolute, publicly accessible URLs to download and play the audio files.

## How the System Works
1. Audio files are uploaded and stored in `wwwroot/uploads/audio/`
2. The file URL is stored as a relative path in the database
3. When creating a robocall campaign, the system generates a webhook URL that includes the audio URL
4. Twilio calls the webhook, which returns TwiML with a `<Play>` tag containing the audio URL
5. Twilio attempts to download the audio file from the URL

## The Issue
The TwiML returned to Twilio contains a relative URL like:
```xml
<Play>/uploads/audio/file.mp3</Play>
```

But Twilio needs an absolute URL like:
```xml
<Play>https://your-domain.com/uploads/audio/file.mp3</Play>
```

## Solution

### Option 1: Fix in FileStorageService (Recommended)
Update the `FileStorageService` to return absolute URLs:

```csharp
// In FileStorageService.cs, modify the UploadAudioAsync method:
public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
{
    // ... existing code ...
    
    // Return absolute URL instead of relative
    var baseUrl = _configuration["Backend:BaseUrl"] ?? 
                  _configuration["Frontend:BaseUrl"] ?? 
                  "http://localhost:5000";
    
    // Ensure baseUrl doesn't have trailing slash
    baseUrl = baseUrl.TrimEnd('/');
    
    var absoluteUrl = $"{baseUrl}/uploads/audio/{uniqueFileName}";
    
    _logger.LogInformation("Audio file uploaded successfully: {FileName}, URL: {Url}", 
        uniqueFileName, absoluteUrl);
    
    return absoluteUrl;
}
```

### Option 2: Fix in TwilioWebhookController
Update the webhook to convert relative URLs to absolute:

```csharp
[HttpPost("voice")]
[HttpGet("voice")]
public IActionResult VoiceResponse([FromQuery] string message = "", [FromQuery] string audioUrl = "")
{
    try
    {
        string twiml;

        if (!string.IsNullOrEmpty(audioUrl))
        {
            // Convert relative URL to absolute if needed
            if (audioUrl.StartsWith("/"))
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                audioUrl = $"{baseUrl}{audioUrl}";
            }
            
            _logger.LogInformation($"Voice response using audio file: {audioUrl}");
            
            twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Play>{System.Security.SecurityElement.Escape(audioUrl)}</Play>
</Response>";
        }
        // ... rest of the method
    }
    // ...
}
```

### Option 3: Use Cloud Storage (Production)
For production deployments, use S3 or DigitalOcean Spaces which already return absolute URLs:
1. Configure `AWS__S3__UseS3=true` in environment variables
2. The S3FileStorageService already returns absolute URLs

## Configuration Required

### For Development
Add to `appsettings.Development.json`:
```json
{
  "Backend": {
    "BaseUrl": "https://localhost:5131"
  }
}
```

### For Production
Set environment variable:
```bash
Backend__BaseUrl=https://your-backend-domain.com
```

Or use the existing `FRONTEND_URL` if backend and frontend share the same domain.

## Testing
1. Upload an MP3 file through the voice recordings interface
2. Create a robocall campaign using that recording
3. Check the logs to see the generated voice URL
4. Verify the URL is absolute and publicly accessible
5. Test the robocall to confirm audio plays

## Additional Notes
- Ensure your backend is publicly accessible (not behind authentication for the `/uploads` path)
- Twilio supports MP3, WAV, and other audio formats
- Audio files should be accessible via HTTPS in production
- Maximum file size for Twilio is 40MB
- Supported audio formats: MP3, WAV, AIFF, GSM, μ-law