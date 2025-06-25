# SMS Opt-In/Opt-Out Setup Guide

## Overview
This guide will help you set up and configure the SMS opt-in/opt-out system for the Tanveer for Hoover campaign.

## 1. Database Setup

### Apply the Migration
First, ensure your database connection is configured correctly in `appsettings.Development.json` or via environment variables.

```bash
cd backend/HooverCanvassingApi
dotnet ef database update
```

If you get a database connection error:
1. Check your PostgreSQL credentials in `appsettings.Development.json`
2. Ensure PostgreSQL is running: `brew services start postgresql` (macOS)
3. Create the database if needed: `createdb hoover_canvassing`

## 2. Configuration

### Update Campaign Settings
Edit `backend/HooverCanvassingApi/appsettings.OptIn.json`:

```json
{
  "OptInSettings": {
    "CampaignName": "Tanveer for Hoover",
    "CampaignPhone": "(205) 555-1234",  // Replace with your actual Twilio number
    "OptInWebsiteUrl": "https://t4happ.com/opt-in",  // Your actual opt-in URL
    "DefaultInvitationMessage": "{CampaignName}: Want campaign updates? Text JOIN to {CampaignPhone} or sign up at {OptInWebsiteUrl}. Reply STOP to opt out.",
    "WelcomeMessage": "Welcome to {CampaignName}! You've successfully opted in to receive campaign updates. Reply STOP to opt out at any time. Reply HELP for support.",
    "OptOutMessage": "You have been unsubscribed from {CampaignName} messages. Reply JOIN to resubscribe.",
    "HelpMessage": "{CampaignName}: Reply STOP to unsubscribe. For support, visit tanveerforhoover.com or call {CampaignPhone}. Msg&data rates may apply."
  }
}
```

### Update Twilio Configuration
In `appsettings.Development.json` or environment variables:

```json
"Twilio": {
  "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
  "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
  "FromPhoneNumber": "+12055551234",  // Your Twilio phone number in E.164 format
  "MessagingServiceSid": "MGXXXXXXXX"  // Optional: For better bulk SMS performance
}
```

## 3. Twilio Webhook Configuration

### Configure SMS Webhook in Twilio Console

1. Log into your [Twilio Console](https://console.twilio.com)
2. Navigate to Phone Numbers > Manage > Active Numbers
3. Click on your campaign phone number
4. In the "Messaging" section, configure:
   - **Webhook URL**: `https://your-api-domain.com/api/optin/sms-webhook`
   - **HTTP Method**: POST
   - **Fallback URL**: Leave blank or set a backup URL
   - **Primary Handler Fails**: Email notification (recommended)

### For Local Development (using ngrok)
If testing locally, use ngrok to expose your local API:

```bash
# Install ngrok if you haven't already
brew install ngrok  # macOS

# Start your API
cd backend/HooverCanvassingApi
dotnet run

# In another terminal, expose your API
ngrok http 5000

# Use the ngrok URL in Twilio, e.g.:
# https://abc123.ngrok.io/api/optin/sms-webhook
```

## 4. Testing the System

### Test Web Opt-In Form
1. Visit `https://your-frontend-domain.com/opt-in`
2. Enter a phone number and complete the form
3. Check the database for the new consent record

### Test SMS Keywords
Send these messages to your Twilio number:

1. **Opt-In**: Text "JOIN" or "START"
   - Should receive welcome message
   - Check database for opt-in record

2. **Opt-Out**: Text "STOP"
   - Should receive unsubscribe confirmation
   - Check database for opt-out record

3. **Help**: Text "HELP"
   - Should receive help message with support info

### Test Campaign Messages
Campaign messages will now only be sent to opted-in numbers:

```bash
# Check opt-in status via API
curl -X GET https://your-api-domain.com/api/optin/status/+12055551234 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 5. Sending Opt-In Invitations

### Via API (for admins)
Send opt-in invitations to specific voters:

```bash
# Send to specific voter IDs
curl -X POST https://your-api-domain.com/api/optininvitation/send \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "voterIds": ["voter1", "voter2", "voter3"]
  }'

# Send with filters
curl -X POST https://your-api-domain.com/api/optininvitation/send \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "filter": {
      "zipCodes": ["35244", "35242"],
      "voteFrequency": "frequent",
      "maxRecipients": 100
    }
  }'
```

### Preview before sending:
```bash
curl -X POST https://your-api-domain.com/api/optininvitation/preview \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "filter": {
      "zipCodes": ["35244"],
      "excludeOptedIn": true,
      "excludeOptedOut": true
    }
  }'
```

## 6. Monitoring and Compliance

### Check Opt-In Statistics
```bash
curl -X GET https://your-api-domain.com/api/optininvitation/stats \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Database Queries for Monitoring
```sql
-- View opt-in/out status summary
SELECT sms_consent_status, COUNT(*) 
FROM "Voters" 
WHERE cell_phone IS NOT NULL 
GROUP BY sms_consent_status;

-- Recent opt-ins
SELECT voter_id, timestamp, method, source 
FROM "ConsentRecords" 
WHERE action = 'OptIn' 
ORDER BY timestamp DESC 
LIMIT 20;

-- Recent opt-outs
SELECT voter_id, timestamp, method, raw_message 
FROM "ConsentRecords" 
WHERE action = 'OptOut' 
ORDER BY timestamp DESC 
LIMIT 20;
```

## 7. Compliance Checklist

- [ ] All SMS campaigns check opt-in status before sending
- [ ] Opt-out keywords (STOP, etc.) are handled immediately
- [ ] Welcome messages include opt-out instructions
- [ ] Campaign messages include "Reply STOP to opt out"
- [ ] Consent records are logged with timestamps and sources
- [ ] Web form clearly shows consent language
- [ ] Messages are only sent between 8 AM - 9 PM local time
- [ ] Campaign is registered with Twilio's A2P 10DLC registry

## 8. Troubleshooting

### Common Issues

1. **Webhook not receiving messages**
   - Check Twilio webhook configuration
   - Verify your API is publicly accessible
   - Check Twilio debugger for errors

2. **Database migration fails**
   - Verify PostgreSQL credentials
   - Ensure database exists
   - Check connection string format

3. **SMS not sending**
   - Verify Twilio credentials
   - Check phone number format (must be E.164)
   - Review Twilio account balance
   - Check opt-in status of recipient

### Logs to Check
- API logs: Check for webhook requests and errors
- Twilio Console: Monitor for delivery failures
- Database: Review ConsentRecords for patterns

## 9. Production Deployment

Before going live:
1. Update all phone numbers and URLs to production values
2. Test end-to-end flow with real phone numbers
3. Set up monitoring and alerts
4. Train campaign staff on the opt-in process
5. Prepare opt-in invitation messaging
6. Document support procedures for voter questions

## Support

For technical issues:
- Check API logs for detailed error messages
- Review Twilio debugger for SMS delivery issues
- Monitor ConsentRecords table for opt-in/out activity

For compliance questions:
- Consult with legal counsel
- Review Twilio's messaging compliance guidelines
- Follow TCPA and CTIA best practices