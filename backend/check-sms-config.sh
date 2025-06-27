#!/bin/bash

# Check SMS Opt-In System Configuration

APP_ID="4210df4e-200d-4397-82d5-c3157127f965"

echo "=== SMS Opt-In System Configuration Check ==="
echo ""

# Check if doctl is installed
if ! command -v doctl &> /dev/null; then
    echo "❌ doctl is not installed. Please install it first."
    exit 1
fi

echo "1. Fetching current environment variables..."
echo ""

# Get current environment variables
ENV_VARS=$(doctl apps config get $APP_ID 2>&1)

if [ $? -ne 0 ]; then
    echo "❌ Failed to fetch environment variables. Make sure you're authenticated with doctl."
    exit 1
fi

echo "2. Checking required SMS configuration..."
echo ""

# Check for required Twilio variables
check_var() {
    local var_name=$1
    if echo "$ENV_VARS" | grep -q "$var_name"; then
        echo "✅ $var_name is configured"
    else
        echo "❌ $var_name is MISSING"
    fi
}

# Twilio Configuration
echo "Twilio Configuration:"
check_var "TWILIO__ACCOUNTSID"
check_var "TWILIO__AUTHTOKEN"
check_var "TWILIO__PHONENUMBER"
check_var "TWILIO__MESSAGINGSERVICESID"

echo ""
echo "Opt-In Settings Configuration:"
check_var "OPTINSETTINGS__CAMPAIGNNAME"
check_var "OPTINSETTINGS__CAMPAIGNPHONE"
check_var "OPTINSETTINGS__WEBSITEURL"
check_var "OPTINSETTINGS__WELCOMEMESSAGE"
check_var "OPTINSETTINGS__OPTOUTMESSAGE"
check_var "OPTINSETTINGS__HELPMESSAGE"
check_var "OPTINSETTINGS__INVITATIONMESSAGE"

echo ""
echo "3. Application Health Check..."
echo ""

# Check if the app is running
APP_STATUS=$(doctl apps list --format ID,Status | grep $APP_ID | awk '{print $2}')
echo "App Status: $APP_STATUS"

echo ""
echo "4. Testing Opt-In Endpoint..."
curl -s -o /dev/null -w "API Response: %{http_code}\n" https://t4h-canvas-2uwxt.ondigitalocean.app/api/optin/status/+12055551234

echo ""
echo "=== Configuration Check Complete ==="
echo ""
echo "Next Steps:"
echo "1. If TWILIO__AUTHTOKEN is missing, get it from your Twilio Console"
echo "2. If TWILIO__MESSAGINGSERVICESID is missing, run the add-messaging-service-env.sh script"
echo "3. Configure Twilio webhook URL: https://t4h-canvas-2uwxt.ondigitalocean.app/api/optin/sms-webhook"