# Troubleshooting Robocall Audio Issues

## Common Issues and Solutions

### 1. Audio File Not Playing (Silent Call)

#### Check Audio File Accessibility
The most common issue is that Twilio cannot access the audio file. Test this by:

1. Copy the audio URL from the logs
2. Try to access it in a browser (incognito mode)
3. If you can't access it, neither can Twilio

#### DigitalOcean Spaces Configuration
If using DigitalOcean Spaces (as shown in your logs), ensure:

1. **The bucket is public** or has proper CORS settings:
   ```
   - Go to DigitalOcean Spaces settings
   - Set bucket permissions to "Public"
   - Or configure CORS to allow Twilio's IP ranges
   ```

2. **File permissions are correct**:
   ```
   - Individual files need to be publicly readable
   - Check the file's ACL settings in DigitalOcean
   ```

#### Audio File Format Issues
Twilio has specific requirements:
- **Supported formats**: MP3, WAV, AIFF, GSM, μ-law
- **Maximum file size**: 40MB
- **Bitrate**: 8kbps to 64kbps recommended for phone quality
- **Sample rate**: 8kHz recommended for phone calls

### 2. URL Encoding Issues

Files with spaces or special characters in the name can cause issues:
- `WhatsApp Audio 2025-07-02 at 07.23.27.mp3` ❌
- `whatsapp_audio_20250702_072327.mp3` ✅

The system now automatically encodes URLs, but it's better to avoid spaces in filenames.

### 3. Testing Your Audio Files

#### Method 1: Direct TwiML Test
Test your audio file directly with Twilio by creating a TwiML Bin:

1. Go to Twilio Console > TwiML Bins
2. Create a new TwiML Bin with:
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <Response>
       <Play>YOUR_AUDIO_URL_HERE</Play>
   </Response>
   ```
3. Save and call the number to test

#### Method 2: Test the Webhook
Test your webhook endpoint directly:
```bash
curl "https://your-backend.com/api/TwilioWebhook/voice?audioUrl=YOUR_ENCODED_URL"
```

Check that it returns valid TwiML.

### 4. Debug Steps

1. **Check the logs** for the actual TwiML being returned:
   ```
   Returning TwiML: <?xml version="1.0" encoding="UTF-8"?>...
   ```

2. **Verify the audio URL** in the logs:
   ```
   Voice response using audio file: https://...
   ```

3. **Test the audio file** directly:
   ```bash
   # Download the file to verify it's accessible
   curl -I "YOUR_AUDIO_URL"
   # Should return 200 OK
   ```

4. **Check Twilio's debugger**:
   - Go to Twilio Console > Monitor > Debugger
   - Look for errors related to your calls

### 5. Common Error Messages

#### "Application Error"
- The webhook URL is not accessible
- The webhook returned invalid TwiML
- Server error in your application

#### Silent Call (No Audio)
- Audio file not accessible (403/404 error)
- Invalid audio format
- Network timeout downloading the file

### 6. Quick Fixes

1. **Use HTTPS**: Always use HTTPS URLs for audio files
2. **Avoid spaces**: Rename files to remove spaces before uploading
3. **Test publicly**: Ensure the file is accessible without authentication
4. **Check format**: Convert to MP3 with proper encoding:
   ```bash
   ffmpeg -i input.wav -codec:a libmp3lame -b:a 32k -ar 8000 output.mp3
   ```

### 7. Alternative: Use Text-to-Speech

If audio files continue to fail, use text-to-speech as a fallback:
```csharp
// In your campaign creation, use message instead of VoiceUrl
campaign.Message = "Your message here";
campaign.VoiceUrl = null;
```

### 8. Production Checklist

- [ ] Audio files are stored in a publicly accessible location
- [ ] URLs are absolute (not relative)
- [ ] Files don't have spaces or special characters in names
- [ ] Audio format is MP3 or WAV
- [ ] File size is under 40MB
- [ ] Webhook endpoint is publicly accessible
- [ ] SSL certificate is valid (for HTTPS URLs)
- [ ] CORS is configured if needed