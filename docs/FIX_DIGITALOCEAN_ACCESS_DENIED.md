# Fix DigitalOcean Spaces Access Denied Error

## Problem
Audio files stored in DigitalOcean Spaces are returning "AccessDenied" errors, preventing Twilio from playing robocall audio.

## Quick Solution

### Option 1: Make Entire Space Public (Easiest)

1. Log into DigitalOcean
2. Go to Spaces → `hoover-canvassing-audio`
3. Click on "Settings" tab
4. Find "File Listing" section
5. Change from "Private" to "Public"
6. Click "Save"

### Option 2: Set Public Access via Bucket Policy

1. In your Space settings, click "Bucket Policy"
2. Add this policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": {
        "AWS": "*"
      },
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::hoover-canvassing-audio/*"
    }
  ]
}
```

3. Click "Save"

### Option 3: Update S3FileStorageService to Set Public ACL

The code already tries to set public access with `CannedACL = S3CannedACL.PublicRead`, but the Space might be overriding this.

Check your Space's default ACL settings:
1. Go to Space settings
2. Look for "Default ACL" or "Object Permissions"
3. Ensure it allows public read access

### Option 4: Use Pre-signed URLs (More Secure)

If you don't want files permanently public, modify the S3FileStorageService:

```csharp
public async Task<string> UploadAudioAsync(Stream audioStream, string fileName)
{
    // ... existing upload code ...
    
    // Instead of returning public URL, generate pre-signed URL
    var request = new Amazon.S3.Model.GetPreSignedUrlRequest
    {
        BucketName = _bucketName,
        Key = key,
        Expires = DateTime.UtcNow.AddDays(7),
        Verb = HttpVerb.GET,
        Protocol = Protocol.HTTPS
    };
    
    var presignedUrl = await _s3Client.GetPreSignedURLAsync(request);
    return presignedUrl;
}
```

## Testing

After making changes:
1. Try accessing the audio URL in a browser (incognito mode)
2. You should be able to download/play the file
3. Test a robocall - it should now play audio

## Security Considerations

- Making files public means anyone with the URL can access them
- Consider using pre-signed URLs for sensitive content
- Use random file names (which you already do) to prevent enumeration
- Set up lifecycle rules to delete old audio files

## Common Issues

### Still Getting Access Denied?
1. Clear CDN cache if using DigitalOcean CDN
2. Wait 1-2 minutes for changes to propagate
3. Check if CORS is blocking access
4. Verify the file exists in the bucket

### File Uploads Still Private?
The Space's default settings might override the ACL set during upload. Check:
1. Space settings → Default Object Permissions
2. Ensure "Public Read" is allowed
3. Or explicitly set bucket policy as shown above