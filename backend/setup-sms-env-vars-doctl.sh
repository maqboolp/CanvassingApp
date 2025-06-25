#!/bin/bash

# Alternative approach using doctl apps update with all env vars at once
# This is more efficient as it updates all variables in a single API call

echo "Setting up SMS Opt-In/Opt-Out Environment Variables using doctl..."

# Configuration - UPDATE THESE VALUES
APP_ID="YOUR_APP_ID"  # Replace with your DigitalOcean app ID
TWILIO_ACCOUNT_SID="YOUR_TWILIO_ACCOUNT_SID"
TWILIO_AUTH_TOKEN="YOUR_TWILIO_AUTH_TOKEN"
TWILIO_PHONE_NUMBER="+12055551234"  # Your Twilio phone in E.164 format
CAMPAIGN_PHONE="(205) 555-1234"  # Display format for messages
OPTIN_WEBSITE_URL="https://t4happ.com/opt-in"

# Optional Twilio Messaging Service (uncomment if using)
# TWILIO_MESSAGING_SERVICE_SID="MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# Get current app spec
echo "Fetching current app configuration..."
doctl apps spec get $APP_ID > app-spec-temp.yaml

# Create the env vars update
cat > env-vars-update.yaml << EOF
envs:
- key: TWILIO__ACCOUNTSID
  value: "$TWILIO_ACCOUNT_SID"
  scope: RUN_TIME
  type: GENERAL
- key: TWILIO__AUTHTOKEN
  value: "$TWILIO_AUTH_TOKEN"
  scope: RUN_TIME
  type: SECRET
- key: TWILIO__FROMPHONENUMBER
  value: "$TWILIO_PHONE_NUMBER"
  scope: RUN_TIME
  type: GENERAL
- key: OPTINSETTINGS__CAMPAIGNNAME
  value: "Tanveer for Hoover"
  scope: RUN_TIME
  type: GENERAL
- key: OPTINSETTINGS__CAMPAIGNPHONE
  value: "$CAMPAIGN_PHONE"
  scope: RUN_TIME
  type: GENERAL
- key: OPTINSETTINGS__OPTINWEBSITEURL
  value: "$OPTIN_WEBSITE_URL"
  scope: RUN_TIME
  type: GENERAL
EOF

# If using Messaging Service, add this to the env-vars-update.yaml:
# - key: TWILIO__MESSAGINGSERVICESID
#   value: "$TWILIO_MESSAGING_SERVICE_SID"
#   scope: RUN_TIME
#   type: GENERAL

echo "Updating environment variables..."
doctl apps update $APP_ID --spec env-vars-update.yaml

# Clean up temporary files
rm -f app-spec-temp.yaml env-vars-update.yaml

echo "Environment variables updated successfully!"
echo ""
echo "The app will now redeploy with the new configuration."
echo "You can check the deployment status with: doctl apps list-deployments $APP_ID"