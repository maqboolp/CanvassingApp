# Configure DigitalOcean Spaces for Public Audio Access

## Quick Fix for Robocall Audio

### 1. Make Your Space Public

1. Log in to DigitalOcean
2. Go to Spaces > `hoover-canvassing-audio`
3. Click on "Settings" tab
4. Under "File Listing", set to "Public"
5. Save changes

### 2. Set Bucket Policy for Public Read Access

Add this bucket policy to allow public read access:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": [
        "s3:GetObject"
      ],
      "Resource": [
        "arn:aws:s3:::hoover-canvassing-audio/audio-memos/*"
      ]
    }
  ]
}
```

Steps:
1. Go to your Space settings
2. Click on "Bucket Policy"
3. Paste the above JSON
4. Click "Save"

### 3. Configure CORS (if needed)

Add CORS configuration to allow Twilio access:

```xml
<CORSConfiguration>
  <CORSRule>
    <AllowedOrigin>*</AllowedOrigin>
    <AllowedMethod>GET</AllowedMethod>
    <AllowedMethod>HEAD</AllowedMethod>
    <MaxAgeSeconds>3000</MaxAgeSeconds>
    <AllowedHeader>*</AllowedHeader>
  </CORSRule>
</CORSConfiguration>
```

### 4. Update File ACL (if needed)

For existing files, you may need to update their ACL:

Using DigitalOcean CLI:
```bash
doctl compute cdn flush <space-name> --files "audio-memos/*"
```

Or using s3cmd:
```bash
s3cmd setacl s3://hoover-canvassing-audio/audio-memos/* --acl-public
```

### 5. Test Access

After making changes, test the audio URL:
1. Copy the URL from your logs
2. Open in an incognito browser window
3. The audio file should download or play

If it works in the browser, it will work with Twilio.

### 6. Alternative: Pre-signed URLs

If you can't make the bucket public, use pre-signed URLs in your S3FileStorageService:

```csharp
public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
{
    // ... upload code ...
    
    // Generate a pre-signed URL valid for 7 days
    var request = new GetPreSignedUrlRequest
    {
        BucketName = _bucketName,
        Key = key,
        Expires = DateTime.UtcNow.AddDays(7),
        Verb = HttpVerb.GET
    };
    
    var url = _s3Client.GetPreSignedURL(request);
    return url;
}
```

### 7. Security Note

Making audio files public means anyone with the URL can access them. Consider:
- Using random file names (which you already do)
- Implementing expiring URLs
- Monitoring access logs
- Deleting old files periodically