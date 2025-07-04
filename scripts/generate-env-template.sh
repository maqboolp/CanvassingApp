#!/bin/bash

# Script to generate environment variable templates
# Usage: ./generate-env-template.sh [backend|frontend|both]

generate_backend() {
    cat << 'EOF'
# Backend Environment Variables Template
# Generated on $(date)

# Database Configuration
ConnectionStrings__DefaultConnection=[YOUR_DATABASE_CONNECTION_STRING]

# JWT Configuration
JwtSettings__Secret=[YOUR_JWT_SECRET_MIN_32_CHARS]
JwtSettings__Issuer=http://localhost:5000
JwtSettings__Audience=http://localhost:5000
JwtSettings__ExpirationMinutes=480

# Email Configuration
EmailSettings__Provider=SendGrid
EmailSettings__SendGridApiKey=[YOUR_SENDGRID_API_KEY]
EmailSettings__FromEmail=[YOUR_FROM_EMAIL]
EmailSettings__FromName=[YOUR_FROM_NAME]

# Campaign Configuration
Campaign__CandidateName=[YOUR_CANDIDATE_NAME]
Campaign__CampaignName=[YOUR_CAMPAIGN_NAME]
Campaign__CampaignTitle=[YOUR_CAMPAIGN_TITLE]
Campaign__PaidForBy=[YOUR_PAID_FOR_BY_TEXT]
Campaign__CampaignEmail=[YOUR_CAMPAIGN_EMAIL]
Campaign__CampaignPhone=[YOUR_CAMPAIGN_PHONE]
Campaign__CampaignWebsite=[YOUR_CAMPAIGN_WEBSITE]
Campaign__CampaignAddress=[YOUR_CAMPAIGN_ADDRESS]
Campaign__Office=[YOUR_OFFICE_SOUGHT]
Campaign__Jurisdiction=[YOUR_JURISDICTION]
Campaign__DefaultCanvassingScript=[YOUR_CANVASSING_SCRIPT]
Campaign__OptInConsentText=[YOUR_CONSENT_TEXT]

# Twilio Configuration (if using SMS)
Twilio__AccountSid=[YOUR_TWILIO_ACCOUNT_SID]
Twilio__AuthToken=[YOUR_TWILIO_AUTH_TOKEN]
Twilio__FromPhoneNumber=[YOUR_TWILIO_PHONE_NUMBER]

# AWS/S3 Configuration (if using cloud storage)
AWS__S3__UseS3=false
AWS__S3__BucketName=[YOUR_S3_BUCKET_NAME]
AWS__S3__AccessKey=[YOUR_S3_ACCESS_KEY]
AWS__S3__SecretKey=[YOUR_S3_SECRET_KEY]
EOF
}

generate_frontend() {
    cat << 'EOF'
# Frontend Environment Variables Template
# Generated on $(date)

# API Configuration
REACT_APP_API_URL=http://localhost:5000

# Branding
REACT_APP_LOGO_URL=[YOUR_LOGO_URL]
REACT_APP_LOGO_ALT=[YOUR_LOGO_ALT_TEXT]
REACT_APP_TITLE=[YOUR_APP_TITLE]
REACT_APP_PRIMARY_COLOR=[YOUR_PRIMARY_COLOR_HEX]
REACT_APP_SECONDARY_COLOR=[YOUR_SECONDARY_COLOR_HEX]

# Campaign Information
REACT_APP_CANDIDATE_NAME=[YOUR_CANDIDATE_NAME]
REACT_APP_CAMPAIGN_NAME=[YOUR_CAMPAIGN_NAME]
REACT_APP_CAMPAIGN_TITLE=[YOUR_CAMPAIGN_TITLE]
REACT_APP_CONSENT_TEXT=[YOUR_CONSENT_TEXT]

# Campaign Resources
REACT_APP_CAMPAIGN_WEBSITE=[YOUR_CAMPAIGN_WEBSITE]
REACT_APP_CAMPAIGN_VENMO=[YOUR_VENMO_HANDLE]
REACT_APP_CAMPAIGN_YOUTUBE=[YOUR_YOUTUBE_URL]
REACT_APP_VOTER_REGISTRATION_URL=[YOUR_VOTER_REG_URL]
REACT_APP_VOLUNTEER_HOTLINE=[YOUR_HOTLINE_NUMBER]
EOF
}

case "$1" in
    backend)
        echo "# Backend Environment Variables"
        generate_backend
        ;;
    frontend)
        echo "# Frontend Environment Variables"
        generate_frontend
        ;;
    both|*)
        echo "# Full Environment Variables Template"
        echo ""
        generate_backend
        echo ""
        echo "# ======================================"
        echo ""
        generate_frontend
        ;;
esac

echo ""
echo "# Note: Replace all [PLACEHOLDER] values with your actual configuration"
echo "# See ENVIRONMENT_VARIABLES.md for detailed documentation"