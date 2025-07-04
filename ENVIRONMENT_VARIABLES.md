# Environment Variables Configuration

This document lists all environment variables that can be configured for the Hoover Canvassing App.

## Backend Environment Variables

### Database Configuration
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
  - Default: `Host=localhost;Database=hoover_canvassing;Username=postgres;Password=postgres123`

### JWT Configuration
- `JwtSettings__Secret` - JWT signing key (minimum 32 characters)
- `JwtSettings__Issuer` - JWT issuer URL
- `JwtSettings__Audience` - JWT audience URL
- `JwtSettings__ExpirationMinutes` - Token expiration in minutes (default: 480)

### Email Configuration
- `EmailSettings__Provider` - Email provider: "SendGrid" or "SMTP"
- `EmailSettings__SendGridApiKey` - SendGrid API key (if using SendGrid)
- `EmailSettings__FromEmail` - From email address
- `EmailSettings__FromName` - From display name
- `EmailSettings__SmtpServer` - SMTP server (if using SMTP)
- `EmailSettings__SmtpPort` - SMTP port (if using SMTP)
- `EmailSettings__Username` - SMTP username (if using SMTP)
- `EmailSettings__Password` - SMTP password (if using SMTP)

### Campaign Configuration (NEW)
- `Campaign__CandidateName` - Candidate's name (e.g., "Tanveer Patel")
- `Campaign__CampaignName` - Campaign name (e.g., "Tanveer for Hoover")
- `Campaign__CampaignTitle` - Full campaign title (e.g., "Tanveer Patel for Hoover City Council")
- `Campaign__PaidForBy` - Legal disclaimer (e.g., "Paid for by Tanveer for Hoover")
- `Campaign__CampaignEmail` - Campaign contact email
- `Campaign__CampaignPhone` - Campaign contact phone
- `Campaign__CampaignWebsite` - Campaign website URL
- `Campaign__CampaignAddress` - Campaign mailing address
- `Campaign__Office` - Office being sought (e.g., "City Council")
- `Campaign__Jurisdiction` - City/County/State (e.g., "City of Hoover")
- `Campaign__DefaultCanvassingScript` - Default script for canvassers
- `Campaign__OptInConsentText` - SMS opt-in consent text

### Twilio Configuration
- `Twilio__AccountSid` - Twilio Account SID
- `Twilio__AuthToken` - Twilio Auth Token
- `Twilio__FromPhoneNumber` - Twilio phone number for SMS
- `Twilio__MessagingServiceSid` - Twilio messaging service SID (optional)

### AWS/S3 Configuration
- `AWS__Region` - AWS region (default: us-east-1)
- `AWS__S3__BucketName` - S3 bucket name for audio files
- `AWS__S3__UseS3` - Enable S3 storage (true/false)
- `AWS__S3__AccessKey` - S3 access key (or DigitalOcean Spaces key)
- `AWS__S3__SecretKey` - S3 secret key
- `AWS__S3__ServiceUrl` - S3 service URL (for DigitalOcean Spaces)
- `AWS__S3__PublicUrl` - Public URL for S3 files

### Application Settings
- `Frontend__BaseUrl` - Frontend application URL
- `ContactProximity__MaxDistanceMeters` - Max distance for proximity check (default: 100)
- `ContactProximity__EnableProximityCheck` - Enable proximity checking (true/false)

## Frontend Environment Variables

### Application Branding
- `REACT_APP_LOGO_URL` - Campaign logo URL (default: '/campaign-logo.png')
- `REACT_APP_LOGO_ALT` - Logo alt text (default: 'Campaign Logo')
- `REACT_APP_TITLE` - App title (default: 'Canvassing App')
- `REACT_APP_PRIMARY_COLOR` - Primary theme color (optional)
- `REACT_APP_SECONDARY_COLOR` - Secondary theme color (optional)

### Campaign Information (NEW - Recommended)
- `REACT_APP_CANDIDATE_NAME` - Candidate's name for display
- `REACT_APP_CAMPAIGN_NAME` - Campaign name for display
- `REACT_APP_CAMPAIGN_TITLE` - Full campaign title
- `REACT_APP_CONSENT_TEXT` - SMS consent text for opt-in forms

### API Configuration
- `REACT_APP_API_URL` - Backend API URL (default: 'http://localhost:5000')

### Campaign Resources
- `REACT_APP_CAMPAIGN_WEBSITE` - Campaign website URL
- `REACT_APP_CAMPAIGN_VENMO` - Venmo handle for donations
- `REACT_APP_CAMPAIGN_YOUTUBE` - YouTube channel URL
- `REACT_APP_VOTER_REGISTRATION_URL` - Voter registration link
- `REACT_APP_VOLUNTEER_HOTLINE` - Volunteer support phone number

## Example .env Files

### Backend (.env)
```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=campaign_db;Username=postgres;Password=secure_password

# JWT
JwtSettings__Secret=your-super-secret-jwt-key-minimum-32-characters-long

# Campaign Settings
Campaign__CandidateName=John Smith
Campaign__CampaignName=Smith for Mayor
Campaign__CampaignTitle=John Smith for Mayor of Springfield
Campaign__PaidForBy=Paid for by Smith for Mayor
Campaign__CampaignEmail=info@smithformayor.com
Campaign__CampaignPhone=555-0123
Campaign__Office=Mayor
Campaign__Jurisdiction=City of Springfield

# Email
EmailSettings__Provider=SendGrid
EmailSettings__SendGridApiKey=SG.xxxxxxxxxxxxx
EmailSettings__FromEmail=noreply@smithformayor.com
EmailSettings__FromName=Smith for Mayor Campaign

# Twilio
Twilio__AccountSid=ACxxxxxxxxxxxxx
Twilio__AuthToken=xxxxxxxxxxxxx
Twilio__FromPhoneNumber=+15550123456
```

### Frontend (.env.local)
```env
# API
REACT_APP_API_URL=http://localhost:5000

# Branding
REACT_APP_LOGO_URL=/smith-logo.png
REACT_APP_TITLE=Smith Campaign App
REACT_APP_PRIMARY_COLOR=#1976d2

# Campaign Info
REACT_APP_CANDIDATE_NAME=John Smith
REACT_APP_CAMPAIGN_NAME=Smith for Mayor
REACT_APP_CAMPAIGN_WEBSITE=https://smithformayor.com
```

## Deployment Notes

1. **Never commit .env files** to version control
2. Use environment-specific configuration:
   - Development: `.env.development`
   - Production: Set via hosting platform (DigitalOcean App Platform, etc.)
3. All sensitive values (API keys, passwords) should use environment variables
4. Campaign-specific text should use the Campaign configuration section
5. For multi-tenant deployments, consider using a configuration service or database-driven settings