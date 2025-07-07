# Complete Environment Variables for DigitalOcean Deployment

## Backend Environment Variables

### Core Settings (Required)
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=${db.CONNECTION_URL}
```

### JWT Authentication (Required)
```bash
JwtSettings__Secret=your-32-char-minimum-secret-key
JwtSettings__Issuer=https://your-app.ondigitalocean.app
JwtSettings__Audience=https://your-app.ondigitalocean.app
JwtSettings__ExpirationMinutes=480
```

### Campaign Configuration (NEW - Required)
These are the new variables we added for campaign customization:
```bash
Campaign__CandidateName=Your Candidate Name
Campaign__CampaignName=Your Campaign Name
Campaign__CampaignTitle=Your Full Campaign Title
Campaign__PaidForBy=Paid for by Your Campaign
Campaign__CampaignEmail=info@yourcampaign.com
Campaign__CampaignPhone=(555) 123-4567
Campaign__CampaignWebsite=https://yourcampaign.com
Campaign__CampaignAddress=Your City, State
Campaign__Office=Office Being Sought
Campaign__Jurisdiction=City/County/State
Campaign__DefaultCanvassingScript=Your default canvassing script here
Campaign__OptInConsentText=Your SMS consent text
```

### Email Settings (Required)
```bash
SENDGRID_API_KEY=SG.your-sendgrid-api-key
EmailSettings__Provider=SendGrid
EmailSettings__FromEmail=noreply@yourcampaign.com
EmailSettings__FromName=Your Campaign Name
Frontend__BaseUrl=https://your-frontend.ondigitalocean.app  # Required for email links
Backend__BaseUrl=https://your-backend.ondigitalocean.app    # Required for file uploads/robocalls
```

### SMS/Twilio Settings (Required for SMS)
```bash
Twilio__AccountSid=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Twilio__AuthToken=your-auth-token
Twilio__FromPhoneNumber=+1234567890
Twilio__MessagingServiceSid=MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### SMS Opt-In Settings (Required for SMS)
These work alongside Campaign settings:
```bash
OPTINSETTINGS__CAMPAIGNNAME=Your Campaign Name
OPTINSETTINGS__CAMPAIGNPHONE=(555) 123-4567
OPTINSETTINGS__OPTINWEBSITEURL=https://your-app.ondigitalocean.app/opt-in
OPTINSETTINGS__DEFAULTINVITATIONMESSAGE=Your Campaign: Want updates? Text JOIN...
OPTINSETTINGS__WELCOMEMESSAGE=Welcome to Your Campaign! You've successfully opted in...
OPTINSETTINGS__OPTOUTMESSAGE=You have been unsubscribed...
OPTINSETTINGS__HELPMESSAGE=Your Campaign: Reply STOP to unsubscribe...
```

### File Storage (Required for audio/photos)
```bash
AWS__S3__UseS3=true
AWS__S3__AccessKey=your-digitalocean-spaces-key
AWS__S3__SecretKey=your-digitalocean-spaces-secret
AWS__S3__ServiceUrl=https://nyc3.digitaloceanspaces.com
AWS__S3__PublicUrl=https://your-bucket.nyc3.digitaloceanspaces.com
AWS__S3__BucketName=your-bucket-name
```

### Optional Services
```bash
GOOGLE_GEOCODING_API_KEY=your-google-api-key  # For voter geocoding
```

## Frontend Environment Variables

Set these in your frontend component:
```bash
REACT_APP_API_URL=https://your-backend.ondigitalocean.app
REACT_APP_CANDIDATE_NAME=Your Candidate Name
REACT_APP_CAMPAIGN_NAME=Your Campaign Name
REACT_APP_CAMPAIGN_TITLE=Your Full Campaign Title
REACT_APP_CAMPAIGN_WEBSITE=https://yourcampaign.com
REACT_APP_CAMPAIGN_VENMO=@YourCampaignVenmo
REACT_APP_CAMPAIGN_YOUTUBE=https://youtube.com/@yourchannel
REACT_APP_CONSENT_TEXT=Your SMS consent text
```

## Your Current T4H Variables

Based on what you showed me, you're currently using:
- Core settings ✓
- JWT settings ✓
- Twilio settings ✓
- OptIn settings ✓
- File storage (DigitalOcean Spaces) ✓
- Google Geocoding ✓

**Missing Campaign__ variables** - These are new and provide better organization for campaign-specific text throughout the app. They work alongside your existing OPTINSETTINGS variables.

## Migration Note

The `Campaign__` variables are used by:
- Email templates (for dynamic campaign info)
- API responses
- Default values throughout the app

The `OPTINSETTINGS__` variables are specifically for SMS opt-in functionality.

Both sets work together - you should set both for a complete configuration.