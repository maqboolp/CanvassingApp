# Environment Variables for Campaign Deployment

This document describes all environment variables needed to deploy the canvassing app for a new campaign.

## Required Environment Variables

### Frontend URL Configuration
- `FRONTEND_URL` - The full URL where your frontend is deployed (e.g., `https://cindy-campaign.com`)
  - Used for: Email links, JWT issuer/audience, CORS configuration
  - Example: `FRONTEND_URL=https://cindy-campaign.com`

### Campaign Configuration
- `REACT_APP_CAMPAIGN_NAME` - Campaign name (e.g., "Cindy for Cullman")
- `REACT_APP_CANDIDATE_NAME` - Candidate's full name (e.g., "Cindy Myrex")
- `REACT_APP_CAMPAIGN_TITLE` - Full campaign title (e.g., "Cindy Myrex for Alabama House")
- `REACT_APP_PAID_FOR_BY` - Campaign finance disclosure (e.g., "Paid for by Cindy for Cullman")
- `REACT_APP_OFFICE` - Office being sought (e.g., "Alabama House")
- `REACT_APP_JURISDICTION` - Jurisdiction (e.g., "City of Cullman")

### Database Configuration
- `DB_USER` - PostgreSQL username
- `DB_PASSWORD` - PostgreSQL password
- `DATABASE_URL` - Full PostgreSQL connection string (if using hosted database)

### Security Configuration
- `JWT_SECRET` - Secret key for JWT tokens (minimum 32 characters)

## Optional Environment Variables

### Email Configuration
- `EMAIL_FROM_ADDRESS` - Email address for sending emails (e.g., `noreply@cindyforcullman.com`)
- `SENDGRID_API_KEY` - SendGrid API key for email delivery

### CORS Configuration
- `CORS_ORIGINS` - Comma-separated list of additional allowed origins
  - Example: `CORS_ORIGINS=https://cindyforcullman.com,https://www.cindyforcullman.com`

### Campaign Resources (Optional)
- `REACT_APP_CAMPAIGN_WEBSITE` - Campaign website URL
- `REACT_APP_CAMPAIGN_EMAIL` - Campaign contact email
- `REACT_APP_CAMPAIGN_PHONE` - Campaign phone number
- `REACT_APP_CAMPAIGN_VENMO` - Venmo handle for donations
- `REACT_APP_CAMPAIGN_YOUTUBE` - YouTube channel URL
- `REACT_APP_VOLUNTEER_HOTLINE` - Volunteer support phone number
- `REACT_APP_VOTER_REGISTRATION_URL` - Voter registration check URL
- `REACT_APP_SUPPORT_EMAIL` - App support email

### SMS Configuration (Optional)
- `TWILIO_ACCOUNT_SID` - Twilio account SID
- `TWILIO_AUTH_TOKEN` - Twilio auth token
- `TWILIO_FROM_PHONE` - Twilio phone number

### File Storage (Optional)
- `AWS_S3_BUCKET_NAME` - S3 bucket name for audio storage
- `AWS_S3_ACCESS_KEY` - AWS access key
- `AWS_S3_SECRET_KEY` - AWS secret key
- `AWS_S3_SERVICE_URL` - S3 service URL (for DigitalOcean Spaces)

## Example .env File for Cindy's Campaign

```bash
# Frontend URL (REQUIRED)
FRONTEND_URL=https://cindy-campaign.com

# Campaign Configuration
REACT_APP_CAMPAIGN_NAME=Cindy for Cullman
REACT_APP_CANDIDATE_NAME=Cindy Myrex
REACT_APP_CAMPAIGN_TITLE=Cindy Myrex for Alabama House
REACT_APP_PAID_FOR_BY=Paid for by Cindy for Cullman
REACT_APP_OFFICE=Alabama House
REACT_APP_JURISDICTION=City of Cullman

# Database
DB_USER=postgres
DB_PASSWORD=your_secure_password

# Security
JWT_SECRET=your-super-secret-jwt-key-minimum-32-characters

# Email
EMAIL_FROM_ADDRESS=noreply@cindyforcullman.com
SENDGRID_API_KEY=your_sendgrid_api_key

# CORS
CORS_ORIGINS=https://cindyforcullman.com,https://www.cindyforcullman.com

# Campaign Resources
REACT_APP_CAMPAIGN_WEBSITE=https://cindyforcullman.com
REACT_APP_CAMPAIGN_EMAIL=info@cindyforcullman.com
REACT_APP_CAMPAIGN_PHONE=205-555-0100
```

## Notes

1. The `FRONTEND_URL` is critical - it's used for:
   - Email links (login buttons, etc.)
   - JWT token validation
   - CORS configuration
   
2. All `REACT_APP_*` variables are also used by the backend for email templates and other features

3. If deploying to DigitalOcean App Platform, these can be set in the App Settings

4. For local development, create a `.env` file in the deployment directory