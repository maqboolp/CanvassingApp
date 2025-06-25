#!/bin/bash

# Script to check if SMS environment variables are properly set

echo "Checking SMS Opt-In/Opt-Out Environment Variables..."
echo "=================================================="

# Update with your app ID
APP_ID="YOUR_APP_ID"

# Get all environment variables
echo "Fetching current environment variables..."
doctl apps config get $APP_ID > env-vars-current.txt

echo ""
echo "SMS-Related Environment Variables:"
echo "----------------------------------"

# Check Twilio variables
echo "Twilio Configuration:"
grep -E "TWILIO__" env-vars-current.txt || echo "  ❌ No Twilio variables found"

echo ""
echo "Opt-In Settings:"
grep -E "OPTINSETTINGS__" env-vars-current.txt || echo "  ❌ No OptIn settings found"

echo ""
echo "Required Variables Checklist:"
echo "-----------------------------"

# Check each required variable
check_var() {
    if grep -q "$1" env-vars-current.txt; then
        echo "✅ $1 is set"
    else
        echo "❌ $1 is MISSING"
    fi
}

check_var "TWILIO__ACCOUNTSID"
check_var "TWILIO__AUTHTOKEN"
check_var "TWILIO__FROMPHONENUMBER"
check_var "OPTINSETTINGS__CAMPAIGNNAME"
check_var "OPTINSETTINGS__CAMPAIGNPHONE"
check_var "OPTINSETTINGS__OPTINWEBSITEURL"

echo ""
echo "Optional Variables:"
echo "------------------"
check_var "TWILIO__MESSAGINGSERVICESID"
check_var "OPTINSETTINGS__DEFAULTINVITATIONMESSAGE"
check_var "OPTINSETTINGS__WELCOMEMESSAGE"
check_var "OPTINSETTINGS__OPTOUTMESSAGE"
check_var "OPTINSETTINGS__HELPMESSAGE"

# Clean up
rm -f env-vars-current.txt

echo ""
echo "=================================================="
echo "To set missing variables, update and run setup-sms-env-vars.sh"