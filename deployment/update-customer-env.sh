#!/bin/bash

# Update environment variables for a deployed customer app
# Usage: ./update-customer-env.sh <app-id>

set -e

if [ -z "$1" ]; then
    echo "Error: App ID is required"
    echo "Usage: ./update-customer-env.sh <app-id>"
    exit 1
fi

APP_ID=$1

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}Updating environment variables for app: ${APP_ID}${NC}"

# Get current app spec
echo "Fetching current app configuration..."
doctl apps spec get ${APP_ID} > current-spec.yaml

# Create a template for environment variables
cat > env-template.txt <<EOF
# Update these environment variables for your customer
# Then run: ./update-customer-env.sh ${APP_ID}

# REQUIRED - Security
JWT_SECRET="generate-a-secure-secret-minimum-32-characters-here"

# REQUIRED - Email (SendGrid)
SENDGRID_API_KEY="SG.your-sendgrid-api-key-here"

# REQUIRED - SMS (Twilio)
TWILIO_ACCOUNT_SID="ACyour-twilio-account-sid"
TWILIO_AUTH_TOKEN="your-twilio-auth-token"
TWILIO_PHONE_NUMBER="+1234567890"

# OPTIONAL - File Storage (DigitalOcean Spaces)
DO_SPACES_KEY="your-spaces-access-key"
DO_SPACES_SECRET="your-spaces-secret-key"
DO_SPACES_ENDPOINT="nyc3.digitaloceanspaces.com"
DO_SPACES_BUCKET="your-bucket-name"

# OPTIONAL - Custom Domain
CUSTOM_DOMAIN="yourcampaign.com"
FRONTEND_URL="https://app.yourcampaign.com"
EOF

echo -e "${GREEN}Environment template created: env-template.txt${NC}"
echo -e "${BLUE}Please update the values in env-template.txt, then run this script again.${NC}"

# Check if env vars are set
read -p "Have you updated env-template.txt with your values? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

# Read environment variables
source env-template.txt

# Update app with new environment variables
echo -e "${BLUE}Updating app environment variables...${NC}"

# Create updated spec with environment variables
cat > updated-spec.yaml <<EOF
services:
- name: api
  envs:
  - key: JwtSettings__Secret
    value: "${JWT_SECRET}"
    type: SECRET
  - key: SENDGRID_API_KEY
    value: "${SENDGRID_API_KEY}"
    type: SECRET
  - key: Twilio__AccountSid
    value: "${TWILIO_ACCOUNT_SID}"
    type: SECRET
  - key: Twilio__AuthToken
    value: "${TWILIO_AUTH_TOKEN}"
    type: SECRET
  - key: Twilio__FromPhoneNumber
    value: "${TWILIO_PHONE_NUMBER}"
  - key: FileStorage__Provider
    value: "S3"
  - key: FileStorage__S3__AccessKey
    value: "${DO_SPACES_KEY}"
    type: SECRET
  - key: FileStorage__S3__SecretKey
    value: "${DO_SPACES_SECRET}"
    type: SECRET
  - key: FileStorage__S3__ServiceUrl
    value: "https://${DO_SPACES_ENDPOINT}"
  - key: FileStorage__S3__BucketName
    value: "${DO_SPACES_BUCKET}"
EOF

# Apply the update
doctl apps update ${APP_ID} --spec updated-spec.yaml

echo -e "${GREEN}Environment variables updated successfully!${NC}"

# Trigger new deployment
echo -e "${BLUE}Triggering new deployment...${NC}"
DEPLOYMENT_ID=$(doctl apps create-deployment ${APP_ID} --format ID --no-header)

echo -e "${GREEN}Deployment initiated: ${DEPLOYMENT_ID}${NC}"
echo "Monitor deployment progress:"
echo "  doctl apps logs ${APP_ID} --follow"
echo "  doctl apps get-deployment ${APP_ID} ${DEPLOYMENT_ID}"

# Clean up
rm -f current-spec.yaml updated-spec.yaml