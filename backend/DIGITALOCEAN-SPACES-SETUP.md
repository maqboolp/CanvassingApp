# DigitalOcean Spaces Setup for Audio Storage

## Overview
DigitalOcean Spaces is an S3-compatible object storage service that provides a cost-effective and scalable solution for storing audio files. The application now supports both AWS S3 and DigitalOcean Spaces.

## Benefits of DigitalOcean Spaces
- **S3-Compatible**: Uses the same API as AWS S3
- **Simpler Pricing**: $5/month for 250GB storage and 1TB bandwidth
- **Built-in CDN**: Free CDN included with every Space
- **Easy Integration**: Works with existing S3 SDKs
- **DigitalOcean Integration**: Seamless integration with other DO services

## Setup Instructions

### 1. Create a DigitalOcean Space
1. Log in to your DigitalOcean account
2. Navigate to "Spaces" in the control panel
3. Click "Create a Space"
4. Configure your Space:
   - **Region**: Choose the closest region (e.g., `nyc3`, `sfo3`)
   - **Space name**: `hoover-canvassing-audio` (or your preferred name)
   - **File listing**: Keep it restricted (default)
   - **CDN**: Enable CDN for better performance

### 2. Generate Access Keys
1. Go to API → Tokens/Keys in your DigitalOcean control panel
2. In the "Spaces access keys" section, click "Generate New Key"
3. Give it a name: `hoover-canvassing-app`
4. Save the Access Key and Secret Key securely

### 3. Configure CORS (Important!)
1. Go to your Space settings
2. Click on "Settings" → "CORS Configuration"
3. Add the following CORS rules:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<CORSConfiguration>
  <CORSRule>
    <AllowedOrigin>https://t4h-canvas-2uwxt.ondigitalocean.app</AllowedOrigin>
    <AllowedOrigin>https://t4happ.com</AllowedOrigin>
    <AllowedOrigin>https://www.t4happ.com</AllowedOrigin>
    <AllowedMethod>GET</AllowedMethod>
    <AllowedMethod>PUT</AllowedMethod>
    <AllowedMethod>POST</AllowedMethod>
    <AllowedMethod>DELETE</AllowedMethod>
    <AllowedHeader>*</AllowedHeader>
    <MaxAgeSeconds>3000</MaxAgeSeconds>
  </CORSRule>
</CORSConfiguration>
```

### 4. Configure the Application

#### For Development (appsettings.json):
```json
"AWS": {
  "Region": "us-east-1",
  "S3": {
    "BucketName": "hoover-canvassing-audio",
    "AudioPrefix": "audio-memos/",
    "UseS3": true,
    "AccessKey": "YOUR_DO_ACCESS_KEY",
    "SecretKey": "YOUR_DO_SECRET_KEY",
    "ServiceUrl": "https://nyc3.digitaloceanspaces.com",
    "PublicUrl": "https://hoover-canvassing-audio.nyc3.digitaloceanspaces.com"
  }
}
```

#### For Production (Environment Variables):
```bash
# Enable S3/Spaces
AWS__S3__UseS3=true

# DigitalOcean Spaces credentials
AWS__S3__AccessKey=YOUR_DO_ACCESS_KEY
AWS__S3__SecretKey=YOUR_DO_SECRET_KEY

# DigitalOcean Spaces configuration
AWS__S3__ServiceUrl=https://nyc3.digitaloceanspaces.com
AWS__S3__PublicUrl=https://hoover-canvassing-audio.nyc3.digitaloceanspaces.com
AWS__S3__BucketName=hoover-canvassing-audio
```

### 5. CDN Configuration (Optional but Recommended)
If you enabled CDN for your Space:
1. Your files will be available at: `https://hoover-canvassing-audio.nyc3.cdn.digitaloceanspaces.com`
2. Update the `PublicUrl` configuration to use the CDN URL:
```bash
AWS__S3__PublicUrl=https://hoover-canvassing-audio.nyc3.cdn.digitaloceanspaces.com
```

## Region Endpoints
Choose the endpoint based on your Space's region:
- NYC3: `https://nyc3.digitaloceanspaces.com`
- SFO3: `https://sfo3.digitaloceanspaces.com`
- AMS3: `https://ams3.digitaloceanspaces.com`
- SGP1: `https://sgp1.digitaloceanspaces.com`
- FRA1: `https://fra1.digitaloceanspaces.com`

## Testing the Integration
1. Set `UseS3: true` in your configuration
2. Add your DigitalOcean Spaces credentials
3. Run the application
4. Record a voice memo in ContactModal
5. Check your DigitalOcean Space for the uploaded file
6. Verify playback works in contact history

## Cost Comparison
### DigitalOcean Spaces:
- Fixed price: $5/month
- Includes: 250GB storage + 1TB bandwidth
- Additional storage: $0.02/GB
- Additional bandwidth: $0.01/GB

### AWS S3:
- Storage: ~$0.023/GB/month
- Requests: ~$0.0004 per 1,000 requests
- Data transfer: First 100GB/month free, then ~$0.09/GB

For most use cases, DigitalOcean Spaces is more cost-effective.

## Migration from AWS S3
To switch from AWS S3 to DigitalOcean Spaces:
1. Create your Space and configure as above
2. Update only the configuration values:
   - `ServiceUrl`: Your DO Spaces endpoint
   - `PublicUrl`: Your Space's public URL
   - `AccessKey` and `SecretKey`: Your DO credentials
3. The application code remains unchanged!

## Troubleshooting
1. **403 Forbidden errors**: Check CORS configuration
2. **404 Not Found**: Verify bucket name and region
3. **Connection refused**: Check ServiceUrl is correct
4. **Files not publicly accessible**: Ensure files are uploaded with public-read ACL

## Security Notes
- Never commit credentials to source control
- Use environment variables in production
- Consider creating a separate Space for each environment
- Enable Spaces CDN for better performance and security