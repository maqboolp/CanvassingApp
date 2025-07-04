# Quick Deployment Guide - DigitalOcean

Since Docker files are already set up, deployment is automatic when you push to Git.

## Required Environment Variables in DigitalOcean

Go to your App → Settings → Component → Environment Variables and add:

### Backend Component
```
DATABASE_URL: ${db.DATABASE_URL}
ConnectionStrings__DefaultConnection: ${db.DATABASE_URL}
JwtSettings__Secret: [generate-32-char-key]
Campaign__CandidateName: John Smith
Campaign__CampaignName: Smith for Mayor
EmailSettings__SendGridApiKey: SG.xxxxx
ASPNETCORE_URLS: http://+:5000
```

### Frontend Component  
```
REACT_APP_API_URL: https://${APP_NAME}-backend.ondigitalocean.app
REACT_APP_CANDIDATE_NAME: John Smith
REACT_APP_CAMPAIGN_NAME: Smith for Mayor
```

## That's it!

Push to Git and DigitalOcean handles the rest. The app will automatically:
- Build using the existing Dockerfiles
- Deploy with your environment variables
- Connect to your DigitalOcean database

## Generate JWT Secret
```bash
openssl rand -base64 32
```

Total setup time: ~5 minutes