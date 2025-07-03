# Customer Configuration Guide

This guide documents all available configuration settings for customizing the Hoover Canvassing App for different customers.

## Table of Contents
- [Environment Variables](#environment-variables)
- [Branding Configuration](#branding-configuration)
- [Campaign Information](#campaign-information)
- [Voter Resources](#voter-resources)
- [API Configuration](#api-configuration)
- [Third-Party Services](#third-party-services)
- [Example Configurations](#example-configurations)

## Environment Variables

### Frontend Environment Variables

These variables control the customer-facing branding and content:

#### Basic Branding
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `REACT_APP_CUSTOMER_ID` | Unique customer identifier | `cindymyrex` | Yes |
| `REACT_APP_LOGO_URL` | Path to customer logo | `/customers/cindymyrex/assets/logo.png` | Yes |
| `REACT_APP_APP_TITLE` | Application title (shows in header and tab) | `Cindy Myrex Canvas` | Yes |
| `REACT_APP_API_URL` | Backend API URL | `https://canvassing-cindymyrex.ondigitalocean.app/api` | Yes |

#### Campaign Information (Login Page)
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `REACT_APP_CAMPAIGN_SLOGAN` | Campaign tagline/slogan | `"Vote for Change"` | No |
| `REACT_APP_CAMPAIGN_MESSAGE` | Campaign message/details | `Join the movement for Alabama House District 12` | No |
| `REACT_APP_CAMPAIGN_DISCLAIMER` | Legal disclaimer | `Paid for by Friends of Cindy Myrex` | No |
| `REACT_APP_CAMPAIGN_WEBSITE` | Campaign website URL | `https://cindymyrex.com` | No |
| `REACT_APP_CAMPAIGN_VENMO` | Venmo handle for donations | `@cindymyrex` | No |
| `REACT_APP_CAMPAIGN_YOUTUBE` | YouTube channel URL | `https://youtube.com/@cindymyrex` | No |

#### Voter Resources (Login Page & Dashboard)
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `REACT_APP_VOTER_REGISTRATION_URL` | Voter registration check URL | `https://myinfo.alabamavotes.gov/VoterView` | No |
| `REACT_APP_VOLUNTEER_HOTLINE` | Support phone number | `(205) 555-1234` | No |

### Backend Environment Variables

These variables control server-side functionality:

#### Database Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Database=cindymyrex;Username=...` | Yes |

#### Authentication
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `JwtSettings__Secret` | JWT secret key (min 32 chars) | `your-super-secret-key-minimum-32-characters` | Yes |
| `JwtSettings__Issuer` | JWT issuer URL | `https://canvassing-cindymyrex.ondigitalocean.app` | Yes |
| `JwtSettings__Audience` | JWT audience URL | `https://canvassing-cindymyrex.ondigitalocean.app` | Yes |
| `JwtSettings__ExpirationMinutes` | Token expiration time | `480` | Yes |

#### Third-Party Services
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `SENDGRID_API_KEY` | SendGrid API key for emails | `SG.xxxxx` | Yes |
| `Twilio__AccountSid` | Twilio account SID | `ACxxxxx` | Yes |
| `Twilio__AuthToken` | Twilio auth token | `xxxxx` | Yes |
| `Twilio__FromPhoneNumber` | Twilio phone number | `+12055551234` | Yes |
| `Twilio__MessagingServiceSid` | Twilio messaging service SID | `MGxxxxx` | No |
| `GOOGLE_GEOCODING_API_KEY` | Google Maps API key for geocoding | `AIzaxxxxx` | Yes |

#### CORS Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `CORS__AllowedOrigins__0` | Primary frontend URL | `https://canvassing-cindymyrex.ondigitalocean.app` | Yes |
| `CORS__AllowedOrigins__1` | Development URL | `http://localhost:3000` | No |

#### Storage Configuration (Optional)
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `AWS__S3__UseS3` | Enable S3/Spaces storage | `true` | No |
| `AWS__S3__AccessKey` | S3 access key | `xxxxx` | If UseS3=true |
| `AWS__S3__SecretKey` | S3 secret key | `xxxxx` | If UseS3=true |
| `AWS__S3__BucketName` | S3 bucket name | `canvassing-cindymyrex` | If UseS3=true |
| `AWS__S3__ServiceUrl` | S3 endpoint URL | `https://nyc3.digitaloceanspaces.com` | If UseS3=true |

## Branding Configuration

### Logo Requirements
- **Format**: PNG with transparent background
- **Recommended Size**: 300x100 pixels
- **Location**: `/customers/{customer-id}/assets/logo.png`
- **Usage**: Displayed in app header and login page

### Color Scheme
Currently uses default purple theme. Future enhancement will allow custom colors via:
- `primaryColor`: Main brand color
- `secondaryColor`: Accent color

## Campaign Information

The campaign information box on the login page is fully customizable:

1. **Campaign Box**: Only displays if slogan or message is configured
2. **Website/Social Links**: Only show if URLs are provided
3. **Venmo QR Code**: Automatically generated from Venmo handle

## Voter Resources

Voter resources section is optional and only displays if configured:

1. **Voter Registration**: Links to state-specific voter registration check
2. **Volunteer Hotline**: Campaign-specific support number
3. **Quick Tips**: Currently static, will be configurable in future

Note: These resources appear both on the login page and in the Dashboard's Resources tab. Campaign website, YouTube, and Venmo information also appears in the Dashboard's Resources tab when configured.

## API Configuration

### Endpoints
The frontend automatically constructs API endpoints based on `REACT_APP_API_URL`:
- Authentication: `{API_URL}/auth/*`
- Voters: `{API_URL}/voters/*`
- Campaigns: `{API_URL}/campaigns/*`

## Third-Party Services

### SendGrid (Email)
Used for:
- Password reset emails
- Volunteer invitations
- Campaign emails

### Twilio (SMS/Voice)
Used for:
- SMS campaigns
- Robocalls
- Voter opt-in messages

### Google Maps (Geocoding)
Used for:
- Converting addresses to coordinates
- Finding nearest voters
- Location-based features

## Example Configurations

### Cindy Myrex Example
```env
# Frontend
REACT_APP_CUSTOMER_ID=cindymyrex
REACT_APP_LOGO_URL=/customers/cindymyrex/assets/logo.png
REACT_APP_APP_TITLE=Cindy Myrex Canvas
REACT_APP_API_URL=https://canvassing-cindymyrex.ondigitalocean.app/api
REACT_APP_CAMPAIGN_SLOGAN="Vote for Change"
REACT_APP_CAMPAIGN_MESSAGE=Cindy Myrex for Alabama House District 12 - November 2024
REACT_APP_CAMPAIGN_DISCLAIMER=Paid for by Friends of Cindy Myrex
REACT_APP_CAMPAIGN_WEBSITE=https://cindymyrex.com
REACT_APP_CAMPAIGN_VENMO=@cindymyrex
REACT_APP_VOTER_REGISTRATION_URL=https://myinfo.alabamavotes.gov/VoterView
REACT_APP_VOLUNTEER_HOTLINE=(205) 555-1234

# Backend
JwtSettings__Secret=cindy-myrex-secret-key-minimum-32-characters-long
SENDGRID_API_KEY=SG.your-sendgrid-key
Twilio__AccountSid=ACyour-twilio-sid
Twilio__AuthToken=your-twilio-auth
Twilio__FromPhoneNumber=+12055551234
GOOGLE_GEOCODING_API_KEY=AIzayour-google-key
```

### Minimal Configuration
```env
# Required only
REACT_APP_CUSTOMER_ID=johndoe
REACT_APP_LOGO_URL=/customers/johndoe/assets/logo.png
REACT_APP_APP_TITLE=John Doe Canvas
REACT_APP_API_URL=https://canvassing-johndoe.ondigitalocean.app/api
JwtSettings__Secret=john-doe-secret-key-minimum-32-characters-long
SENDGRID_API_KEY=SG.your-key
Twilio__AccountSid=ACyour-sid
Twilio__AuthToken=your-auth
Twilio__FromPhoneNumber=+1234567890
GOOGLE_GEOCODING_API_KEY=AIzayour-key
```

## Setting Environment Variables

### DigitalOcean App Platform
1. Go to your app in DigitalOcean dashboard
2. Click on "Settings" tab
3. Scroll to "App-Level Environment Variables" or component-specific variables
4. Add each variable with its value
5. Click "Save" and the app will redeploy

### Local Development
Create `.env` files:
- Frontend: `/frontend/.env.local`
- Backend: `/backend/HooverCanvassingApi/appsettings.Development.json`

## Best Practices

1. **Security**:
   - Never commit API keys or secrets to Git
   - Use strong, unique JWT secrets per customer
   - Rotate API keys regularly

2. **Branding**:
   - Always provide a logo
   - Keep campaign messages concise
   - Test all links before deployment

3. **Optional Features**:
   - Only configure what's needed
   - Leave optional fields empty if not used
   - Campaign info box won't show if not configured

## Troubleshooting

### Logo Not Showing
- Check file exists at specified path
- Verify REACT_APP_LOGO_URL is correct
- Ensure logo is in PNG format

### Campaign Box Missing
- At least one of slogan/message must be set
- Check environment variables are saved
- Redeploy after changes

### API Connection Issues
- Verify REACT_APP_API_URL includes `/api`
- Check CORS configuration matches frontend URL
- Ensure backend is running

## Future Enhancements

Planned configuration options:
- Custom color themes
- Configurable canvassing tips
- Multiple language support
- Custom email templates
- Branding for SMS messages