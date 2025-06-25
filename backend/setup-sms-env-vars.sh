#!/bin/bash

# SMS Opt-In/Opt-Out Environment Variables Setup Script
# This script sets up the required environment variables for the SMS system using doctl

echo "Setting up SMS Opt-In/Opt-Out Environment Variables..."

# Get the app ID - you may need to update this with your actual app ID
APP_ID="YOUR_APP_ID"  # Replace with your DigitalOcean app ID

# You can find your app ID by running:
# doctl apps list

# Function to set an environment variable
set_env_var() {
    local key=$1
    local value=$2
    echo "Setting $key..."
    doctl apps config set $APP_ID --key="$key" --value="$value"
}

# Twilio Configuration
echo "=== Setting Twilio Configuration ==="
set_env_var "TWILIO__ACCOUNTSID" "YOUR_TWILIO_ACCOUNT_SID"
set_env_var "TWILIO__AUTHTOKEN" "YOUR_TWILIO_AUTH_TOKEN"
set_env_var "TWILIO__FROMPHONENUMBER" "+12055551234"  # Replace with your Twilio phone number
# set_env_var "TWILIO__MESSAGINGSERVICESID" "MGxxxxxxxx"  # Uncomment if using Messaging Service

# Opt-In Settings
echo "=== Setting Opt-In Configuration ==="
set_env_var "OPTINSETTINGS__CAMPAIGNNAME" "Tanveer for Hoover"
set_env_var "OPTINSETTINGS__CAMPAIGNPHONE" "(205) 555-1234"  # Replace with your actual phone
set_env_var "OPTINSETTINGS__OPTINWEBSITEURL" "https://t4happ.com/opt-in"

# Optional: Custom Messages (uncomment to customize)
# echo "=== Setting Custom Messages ==="
# set_env_var "OPTINSETTINGS__DEFAULTINVITATIONMESSAGE" "{CampaignName}: Want campaign updates? Text JOIN to {CampaignPhone} or sign up at {OptInWebsiteUrl}. Reply STOP to opt out."
# set_env_var "OPTINSETTINGS__WELCOMEMESSAGE" "Welcome to {CampaignName}! You've successfully opted in to receive campaign updates. Reply STOP to opt out at any time. Reply HELP for support."
# set_env_var "OPTINSETTINGS__OPTOUTMESSAGE" "You have been unsubscribed from {CampaignName} messages. Reply JOIN to resubscribe."
# set_env_var "OPTINSETTINGS__HELPMESSAGE" "{CampaignName}: Reply STOP to unsubscribe. For support, visit tanveerforhoover.com or call {CampaignPhone}. Msg&data rates may apply."

echo "Environment variables setup complete!"
echo ""
echo "IMPORTANT: Before running this script:"
echo "1. Install doctl: brew install doctl (macOS) or download from GitHub"
echo "2. Authenticate: doctl auth init"
echo "3. Find your app ID: doctl apps list"
echo "4. Update the APP_ID variable in this script"
echo "5. Replace all placeholder values with your actual credentials"
echo ""
echo "After running this script, your app will automatically redeploy with the new environment variables."