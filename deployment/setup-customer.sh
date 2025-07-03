#!/bin/bash

# Setup script for new customer
# Usage: ./setup-customer.sh <customer-id> <customer-name> <organization>

set -e

# Check if all parameters are provided
if [ -z "$1" ] || [ -z "$2" ] || [ -z "$3" ]; then
    echo "Error: All parameters are required"
    echo "Usage: ./setup-customer.sh <customer-id> <customer-name> <organization>"
    echo "Example: ./setup-customer.sh cindymyrex 'Cindy Myrex' 'Cindy Myrex for Alabama House'"
    exit 1
fi

CUSTOMER_ID=$1
CUSTOMER_NAME=$2
ORGANIZATION=$3

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}Setting up customer: ${CUSTOMER_NAME} (${CUSTOMER_ID})${NC}"

# Create customer directory structure
echo -e "${BLUE}Creating directory structure...${NC}"
mkdir -p "../customers/${CUSTOMER_ID}/assets"
mkdir -p "../customers/${CUSTOMER_ID}/config"
mkdir -p "../customers/${CUSTOMER_ID}/scripts"

# Create customer configuration file
echo -e "${BLUE}Creating customer configuration...${NC}"
cat > "../customers/${CUSTOMER_ID}/config/customer.json" <<EOF
{
  "customer": {
    "id": "${CUSTOMER_ID}",
    "name": "${CUSTOMER_NAME}",
    "organization": "${ORGANIZATION}"
  },
  "branding": {
    "logoUrl": "/assets/logo.png",
    "logoAlt": "${ORGANIZATION}",
    "appTitle": "${CUSTOMER_NAME} Canvas",
    "primaryColor": "#673de6",
    "secondaryColor": "#ffd700"
  },
  "api": {
    "endpoints": {
      "production": "https://canvassing-${CUSTOMER_ID}.ondigitalocean.app/api",
      "staging": "https://canvassing-${CUSTOMER_ID}-staging.ondigitalocean.app/api"
    }
  },
  "features": {
    "smsEnabled": true,
    "robocallEnabled": true,
    "emailEnabled": true,
    "geocodingEnabled": true
  },
  "metadata": {
    "createdAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "version": "1.0"
  }
}
EOF

# Create placeholder logo
echo -e "${BLUE}Creating placeholder assets...${NC}"
cat > "../customers/${CUSTOMER_ID}/assets/README.md" <<EOF
# ${CUSTOMER_NAME} Assets

Place customer assets in this directory:
- logo.png (recommended: 300x100px, transparent background)
- favicon.ico (optional)
- other branding assets as needed
EOF

# Create deployment configuration
echo -e "${BLUE}Creating deployment configuration...${NC}"
cat > "../customers/${CUSTOMER_ID}/config/deployment.env" <<EOF
# Deployment configuration for ${CUSTOMER_NAME}
# Copy this file to deployment.env.local and fill in the values

# Customer Information
CUSTOMER_ID="${CUSTOMER_ID}"
CUSTOMER_NAME="${CUSTOMER_NAME}"
ORGANIZATION="${ORGANIZATION}"

# DigitalOcean Configuration
DO_APP_NAME="canvassing-${CUSTOMER_ID}"
DO_REGION="nyc"

# Database Configuration (leave empty to create new)
DB_CONNECTION_STRING=""

# API Keys (REQUIRED - fill these in!)
SENDGRID_API_KEY="YOUR_SENDGRID_API_KEY"
TWILIO_ACCOUNT_SID="YOUR_TWILIO_ACCOUNT_SID"
TWILIO_AUTH_TOKEN="YOUR_TWILIO_AUTH_TOKEN"
TWILIO_FROM_PHONE="YOUR_TWILIO_PHONE_NUMBER"
GOOGLE_GEOCODING_API_KEY="YOUR_GOOGLE_API_KEY"

# JWT Configuration
JWT_SECRET="GENERATE_A_SECURE_32_CHAR_MIN_SECRET_HERE"

# AWS S3/DigitalOcean Spaces (optional)
AWS_S3_ACCESS_KEY=""
AWS_S3_SECRET_KEY=""
AWS_S3_BUCKET_NAME=""
AWS_S3_SERVICE_URL="https://nyc3.digitaloceanspaces.com"

# Domain Configuration (optional)
CUSTOM_DOMAIN=""

# Campaign Information
CAMPAIGN_SLOGAN="Join our campaign!"
CAMPAIGN_MESSAGE="Join the movement for positive change"
CAMPAIGN_DISCLAIMER="Paid for by ${ORGANIZATION}"
CAMPAIGN_WEBSITE=""
CAMPAIGN_VENMO=""
CAMPAIGN_YOUTUBE=""

# Voter Resources
VOTER_REGISTRATION_URL=""
VOLUNTEER_HOTLINE=""

# Registration Page
REGISTRATION_TITLE="Join Our Campaign Team"
REGISTRATION_SUBTITLE="Help bring positive change to our community!"
EOF

# Create customer-specific scripts
echo -e "${BLUE}Creating customer scripts...${NC}"
cat > "../customers/${CUSTOMER_ID}/scripts/deploy.sh" <<EOF
#!/bin/bash

# Deploy script for ${CUSTOMER_NAME}
# This script deploys the application for this specific customer

set -e

# Load environment variables
if [ -f "../config/deployment.env.local" ]; then
    source ../config/deployment.env.local
else
    echo "Error: deployment.env.local not found. Copy deployment.env to deployment.env.local and fill in the values."
    exit 1
fi

# Run the main deployment script
cd ../../../deployment
./deploy-new-customer.sh "\${CUSTOMER_NAME}"
EOF

chmod +x "../customers/${CUSTOMER_ID}/scripts/deploy.sh"

# Create documentation
echo -e "${BLUE}Creating documentation...${NC}"
cat > "../customers/${CUSTOMER_ID}/README.md" <<EOF
# ${CUSTOMER_NAME} - Canvassing App Deployment

## Customer Information
- **Customer ID**: ${CUSTOMER_ID}
- **Customer Name**: ${CUSTOMER_NAME}
- **Organization**: ${ORGANIZATION}
- **Created**: $(date -u +"%Y-%m-%d")

## Directory Structure
\`\`\`
customers/${CUSTOMER_ID}/
├── assets/          # Customer branding assets (logo, etc.)
├── config/          # Configuration files
│   ├── customer.json     # Customer configuration
│   └── deployment.env    # Deployment environment template
└── scripts/         # Customer-specific scripts
    └── deploy.sh    # Deployment script
\`\`\`

## Setup Instructions

1. **Add Logo**:
   - Place the customer's logo at \`assets/logo.png\`
   - Recommended size: 300x100px with transparent background

2. **Configure Deployment**:
   - Copy \`config/deployment.env\` to \`config/deployment.env.local\`
   - Fill in all required API keys and configuration values

3. **Deploy**:
   - Run \`scripts/deploy.sh\` to deploy the application

## Environment Variables

The following environment variables must be configured:
- \`SENDGRID_API_KEY\`: For email functionality
- \`TWILIO_ACCOUNT_SID\`, \`TWILIO_AUTH_TOKEN\`, \`TWILIO_FROM_PHONE\`: For SMS/voice
- \`GOOGLE_GEOCODING_API_KEY\`: For address geocoding
- \`JWT_SECRET\`: For authentication (min 32 characters)

## Deployment URLs
- **Production**: https://canvassing-${CUSTOMER_ID}.ondigitalocean.app
- **API**: https://canvassing-${CUSTOMER_ID}.ondigitalocean.app/api

## Support
For deployment issues or questions, refer to the main project documentation.
EOF

echo -e "${GREEN}✓ Customer setup complete!${NC}"
echo -e "\n${BLUE}Next steps:${NC}"
echo "1. Add the customer's logo to: customers/${CUSTOMER_ID}/assets/logo.png"
echo "2. Configure deployment settings in: customers/${CUSTOMER_ID}/config/deployment.env.local"
echo "3. Deploy using: cd customers/${CUSTOMER_ID}/scripts && ./deploy.sh"
echo ""
echo -e "${BLUE}Customer directory created at: customers/${CUSTOMER_ID}/${NC}"