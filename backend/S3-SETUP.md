# S3 Setup for Audio Storage

> **Note**: The application supports both AWS S3 and S3-compatible services like DigitalOcean Spaces. For DigitalOcean Spaces setup, see [DIGITALOCEAN-SPACES-SETUP.md](./DIGITALOCEAN-SPACES-SETUP.md).

## Why S3?
- **Unlimited Storage**: No file system limitations
- **Scalability**: Handles any number of audio files
- **Reliability**: 99.999999999% durability
- **Cost-effective**: Pay only for what you use
- **CDN Ready**: Can integrate with CloudFront for faster delivery

## Configuration

### 1. Create AWS Account and S3 Bucket
1. Go to [AWS Console](https://console.aws.amazon.com)
2. Create a new S3 bucket named `hoover-canvassing-audio`
3. Choose US East 1 (us-east-1) region
4. Leave other settings as default

### 2. Create IAM User for Application
1. Go to IAM service
2. Create new user: `hoover-canvassing-app`
3. Attach policy: `AmazonS3FullAccess` (or create custom policy for specific bucket)
4. Create access keys

### 3. Configure Application

#### For Development (appsettings.json):
```json
"AWS": {
  "Region": "us-east-1",
  "S3": {
    "BucketName": "hoover-canvassing-audio",
    "AudioPrefix": "audio-memos/",
    "UseS3": false,  // Set to true to use S3
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY"
  }
}
```

#### For Production (Environment Variables):
```bash
AWS__S3__UseS3=true
AWS__S3__AccessKey=YOUR_ACCESS_KEY
AWS__S3__SecretKey=YOUR_SECRET_KEY
AWS__S3__BucketName=hoover-canvassing-audio
```

### 4. Custom S3 Policy (Recommended)
Instead of full S3 access, create a policy for just your bucket:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::hoover-canvassing-audio/*",
        "arn:aws:s3:::hoover-canvassing-audio"
      ]
    }
  ]
}
```

## Features Implemented

1. **Automatic Switching**: Uses local storage in development, S3 in production
2. **Public URLs**: Audio files are publicly accessible for playback
3. **Automatic Cleanup**: Deletes audio files older than 90 days
4. **Secure Naming**: Files use GUIDs to prevent naming conflicts
5. **Error Handling**: Falls back gracefully if S3 is unavailable

## Cost Estimation
- Storage: ~$0.023 per GB per month
- Requests: ~$0.0004 per 1,000 requests
- Data Transfer: First 100GB/month free, then ~$0.09 per GB

For 1,000 audio files (avg 1MB each):
- Storage: $0.023/month
- Estimated monthly cost: < $1

## Testing S3 Integration
1. Set `UseS3: true` in appsettings.json
2. Add your AWS credentials
3. Run the application
4. Record a voice memo in ContactModal
5. Check S3 bucket for uploaded file
6. Verify playback in contact history