# Campaign SMS and Robo Call Setup Guide

This guide explains how to set up and use the new SMS and robo call features added to your Tanveer for Hoover campaign app.

## Features Added

### Backend Features
- **Twilio Integration**: Complete SMS and voice call API integration
- **Campaign Management**: Create, schedule, send, and track campaigns
- **Voter Targeting**: Filter recipients by ZIP code, age, vote frequency, and support level
- **Real-time Status Tracking**: Monitor delivery success/failure rates
- **Webhook Support**: Automatic status updates from Twilio
- **Cost Tracking**: Monitor campaign spending

### Frontend Features
- **Campaign Dashboard**: New admin tab for managing campaigns
- **Campaign Creation**: User-friendly form with audience filtering
- **Scheduling**: Schedule campaigns for future delivery
- **Real-time Monitoring**: Track campaign progress and statistics
- **Message Templates**: Support for both SMS and robo call scripts

## Setup Instructions

### 1. Twilio Account Setup
1. Create a Twilio account at https://www.twilio.com
2. Get your Account SID and Auth Token from the Twilio Console
3. Purchase a phone number for sending SMS and making calls
4. Set up your billing information

### 2. Configuration

#### Development Environment
Update `backend/HooverCanvassingApi/appsettings.Development.json`:
```json
{
  "Twilio": {
    "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
    "AuthToken": "YOUR_TWILIO_AUTH_TOKEN", 
    "FromPhoneNumber": "+1234567890"
  }
}
```

#### Production Environment
Set these environment variables in DigitalOcean:
- `TWILIO_ACCOUNT_SID`: Your Twilio Account SID
- `TWILIO_AUTH_TOKEN`: Your Twilio Auth Token
- `TWILIO_FROM_PHONE_NUMBER`: Your Twilio phone number (e.g., +12345678901)

### 3. Database Migration
Run the database migration to create campaign tables:
```bash
cd backend/HooverCanvassingApi
dotnet ef database update
```

### 4. Webhook Configuration (Optional)
For real-time status updates, configure Twilio webhooks:

1. **SMS Status Webhook**: `https://your-domain.com/api/twiliowebhook/sms-status`
2. **Call Status Webhook**: `https://your-domain.com/api/twiliowebhook/call-status`
3. **Voice URL for Robo Calls**: `https://your-domain.com/api/twiliowebhook/voice?message=Your+message+here`

## Using the Campaign Features

### Creating a Campaign

1. **Access the Admin Dashboard**: Log in as a superadmin and go to the "Campaigns" tab (only visible to superadmin users)
2. **Click "Create Campaign"** to open the campaign creation dialog
3. **Fill out the campaign details**:
   - **Name**: Give your campaign a descriptive name
   - **Type**: Choose SMS or Robo Call
   - **Message**: Write your message (1600 character limit for SMS)
   - **Voice URL**: For robo calls, specify TwiML endpoint (optional)

### Audience Targeting

Filter your recipients using these criteria:
- **ZIP Codes**: Target specific areas (comma-separated, e.g., "35244, 35216")
- **Vote Frequency**: Target frequent, infrequent, or non-voters
- **Age Range**: Set minimum and maximum ages
- **Voter Support**: Target based on previous contact outcomes

### Campaign Management

- **Send Now**: Immediately start sending to all recipients
- **Schedule**: Set a future date/time for automatic sending
- **Monitor Progress**: Track delivery statistics in real-time
- **Cancel**: Stop scheduled or in-progress campaigns

### Message Templates

#### SMS Example:
```
Hi [Name], this is Tanveer Patel running for Hoover City Council. Your voice matters in this election. Early voting starts [Date]. Learn more at tanveer4hoover.com. Reply STOP to opt out.
```

#### Robo Call Script Example:
```
Hello, this is a message from Tanveer Patel, candidate for Hoover City Council. I'm calling to remind you that early voting is now available. Your participation in local government makes a difference. For more information, visit tanveer4hoover.com. Thank you for your time.
```

## Compliance and Best Practices

### Legal Compliance
- **TCPA Compliance**: Only contact voters who have provided phone numbers to public records
- **Opt-out Mechanism**: Always include opt-out instructions in SMS
- **Time Restrictions**: Send messages during appropriate hours (9 AM - 9 PM)
- **Local Laws**: Follow Alabama election laws and FCC regulations

### Message Best Practices
- **Keep SMS under 160 characters** when possible
- **Include candidate name and purpose**
- **Always provide opt-out instructions**
- **Personalize when possible**
- **Include website for more information**

### Cost Management
- **SMS**: Typically $0.0075 per message
- **Voice Calls**: Typically $0.0225 per minute
- **Monitor spending**: Use the cost tracking features
- **Test first**: Send to a small group to test before full campaigns

## Troubleshooting

### Common Issues

1. **"Campaign cannot be sent"**
   - Check that campaign is in "Draft" status
   - Verify recipients have phone numbers
   - Ensure Twilio credentials are correct

2. **High failure rates**
   - Check phone number formatting
   - Verify Twilio account has sufficient balance
   - Check for rate limiting

3. **Messages not delivering**
   - Verify webhook configuration
   - Check Twilio logs in their console
   - Ensure phone numbers are valid

### Support Resources
- **Twilio Documentation**: https://www.twilio.com/docs
- **Campaign Analytics**: Use the built-in analytics to track performance
- **Error Logs**: Check the application logs for detailed error messages

## Security Considerations

- **Protect Twilio Credentials**: Never commit credentials to source control
- **Use Environment Variables**: Store sensitive config in environment variables
- **Monitor Usage**: Regularly check Twilio usage and billing
- **Limit Access**: Only superadmin users can create and send campaigns

## Cost Estimation

### Sample Campaign Costs
- **1,000 SMS messages**: ~$7.50
- **1,000 voice calls (30 seconds each)**: ~$11.25
- **Monthly Twilio phone number**: ~$1.00

### Budget Planning
- Calculate costs based on voter list size
- Consider multiple touchpoints throughout campaign
- Factor in testing and failed delivery costs
- Monitor real-time spending in dashboard

## Next Steps

1. **Set up Twilio account** and get credentials
2. **Configure environment variables** for production
3. **Test with small audience** first
4. **Create message templates** for different voter segments
5. **Set up monitoring** and analytics tracking
6. **Train team members** on campaign management features

For technical support or questions about implementation, contact the development team.