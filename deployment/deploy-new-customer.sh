#!/bin/bash

# Deploy Hoover Canvassing App for a new customer
# Usage: ./deploy-new-customer.sh <customer-name>

set -e

# Check if customer name is provided
if [ -z "$1" ]; then
    echo "Error: Customer name is required"
    echo "Usage: ./deploy-new-customer.sh <customer-name>"
    exit 1
fi

CUSTOMER_NAME=$(echo "$1" | tr '[:upper:]' '[:lower:]')
CUSTOMER_ID="${CUSTOMER_NAME// /-}"
APP_NAME="canvassing-${CUSTOMER_ID}"
GITHUB_REPO=${GITHUB_REPO:-"maqboolp/CanvassingApp"}
GITHUB_BRANCH=${GITHUB_BRANCH:-"main"}
REGION=${REGION:-"nyc"}

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}Deploying Hoover Canvassing App for customer: ${CUSTOMER_NAME}${NC}"

# Check if doctl is installed
if ! command -v doctl &> /dev/null; then
    echo -e "${RED}Error: doctl CLI is not installed. Please install it first.${NC}"
    echo "Visit: https://docs.digitalocean.com/reference/doctl/how-to/install/"
    exit 1
fi

# Check if authenticated
if ! doctl account get &> /dev/null; then
    echo -e "${RED}Error: Not authenticated with DigitalOcean. Run: doctl auth init${NC}"
    exit 1
fi

# Create app spec file
echo -e "${BLUE}Creating app specification...${NC}"
cat > /tmp/${APP_NAME}-spec.yaml <<EOF
name: ${APP_NAME}
region: ${REGION}
services:
- name: api
  github:
    repo: ${GITHUB_REPO}
    branch: ${GITHUB_BRANCH}
    deploy_on_push: true
  source_dir: backend
  build_command: dotnet publish -c Release -o out HooverCanvassingApi/HooverCanvassingApi.csproj
  run_command: cd out && dotnet HooverCanvassingApi.dll
  http_port: 8080
  instance_count: 1
  instance_size_slug: basic-xxs
  health_check:
    http_path: /health
    initial_delay_seconds: 30
    period_seconds: 30
  routes:
  - path: /api
  envs:
  - key: ASPNETCORE_URLS
    value: "http://+:8080"
  - key: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - key: ConnectionStrings__DefaultConnection
    value: "\${db.DATABASE_URL}?sslmode=require"
  - key: JwtSettings__Secret
    value: "CHANGE_THIS_TO_SECURE_SECRET_MIN_32_CHARS_${CUSTOMER_NAME}"
  - key: JwtSettings__Issuer
    value: "https://${APP_NAME}.ondigitalocean.app"
  - key: JwtSettings__Audience
    value: "https://${APP_NAME}.ondigitalocean.app"
  - key: JwtSettings__ExpirationMinutes
    value: "480"
  - key: SENDGRID_API_KEY
    value: "YOUR_SENDGRID_API_KEY_HERE"
  - key: Twilio__AccountSid
    value: "YOUR_TWILIO_ACCOUNT_SID"
  - key: Twilio__AuthToken
    value: "YOUR_TWILIO_AUTH_TOKEN"
  - key: Twilio__FromPhoneNumber
    value: "YOUR_TWILIO_PHONE_NUMBER"
  - key: CORS__AllowedOrigins__0
    value: "https://${APP_NAME}-frontend.ondigitalocean.app"
  - key: CORS__AllowedOrigins__1
    value: "http://localhost:3000"

- name: frontend
  github:
    repo: ${GITHUB_REPO}
    branch: ${GITHUB_BRANCH}
    deploy_on_push: true
  source_dir: frontend
  build_command: npm ci && npm run build
  environment_slug: node-js
  http_port: 8080
  instance_count: 1
  instance_size_slug: basic-xxs
  routes:
  - path: /
  envs:
  - key: REACT_APP_API_URL
    value: "https://${APP_NAME}.ondigitalocean.app/api"
  - key: NODE_ENV
    value: "production"
  - key: REACT_APP_CUSTOMER_ID
    value: "${CUSTOMER_ID}"
  - key: REACT_APP_LOGO_URL
    value: "/customers/${CUSTOMER_ID}/assets/logo.png"
  - key: REACT_APP_APP_TITLE
    value: "${CUSTOMER_NAME} Canvas"
  - key: REACT_APP_CAMPAIGN_SLOGAN
    value: "Join our campaign!"
  - key: REACT_APP_CAMPAIGN_MESSAGE
    value: "Join the movement for positive change"
  - key: REACT_APP_CAMPAIGN_DISCLAIMER
    value: "Paid for by ${CUSTOMER_NAME}"
  - key: REACT_APP_CAMPAIGN_WEBSITE
    value: ""
  - key: REACT_APP_CAMPAIGN_VENMO
    value: ""
  - key: REACT_APP_CAMPAIGN_YOUTUBE
    value: ""
  - key: REACT_APP_VOTER_REGISTRATION_URL
    value: ""
  - key: REACT_APP_VOLUNTEER_HOTLINE
    value: ""

databases:
- name: db
  engine: PG
  version: "15"
  size: db-s-1vcpu-1gb
  num_nodes: 1
EOF

# Create the app
echo -e "${BLUE}Creating DigitalOcean App...${NC}"
echo -e "${BLUE}App name: ${APP_NAME}${NC}"

# First, let's check if the app already exists
EXISTING_APP=$(doctl apps list --format Name --no-header | grep -x "${APP_NAME}" || echo "")
if [ -n "$EXISTING_APP" ]; then
    echo -e "${RED}Error: App '${APP_NAME}' already exists!${NC}"
    echo "Choose a different customer name or delete the existing app first."
    exit 1
fi

# Create the app with better error handling
echo "Creating app from spec: /tmp/${APP_NAME}-spec.yaml"
APP_CREATE_OUTPUT=$(doctl apps create --spec /tmp/${APP_NAME}-spec.yaml 2>&1)
APP_CREATE_STATUS=$?

if [ $APP_CREATE_STATUS -ne 0 ]; then
    echo -e "${RED}Error creating app:${NC}"
    echo "$APP_CREATE_OUTPUT"
    echo ""
    echo "Common issues:"
    echo "1. Check if you're authenticated: doctl auth list"
    echo "2. Check your GitHub repo exists: ${GITHUB_REPO}"
    echo "3. Try with a simpler name (lowercase, no special chars)"
    exit 1
fi

APP_ID=$(echo "$APP_CREATE_OUTPUT" | grep -o '[a-f0-9]\{8\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{12\}' | head -1)

echo -e "${GREEN}App created successfully! App ID: ${APP_ID}${NC}"

# Wait for app to be deployed
echo -e "${BLUE}Waiting for initial deployment (this may take 10-15 minutes)...${NC}"
sleep 30

# Get app details
APP_URL=$(doctl apps get ${APP_ID} --format DefaultIngress --no-header)
echo -e "${GREEN}App URL: ${APP_URL}${NC}"

# Create environment file for customer
echo -e "${BLUE}Creating customer environment file...${NC}"
cat > ${CUSTOMER_NAME}-env.txt <<EOF
# Environment variables for ${CUSTOMER_NAME}
# IMPORTANT: Update these values before deploying!

APP_ID=${APP_ID}
APP_NAME=${APP_NAME}
APP_URL=${APP_URL}
CUSTOMER_NAME=${CUSTOMER_NAME}

# Required environment variables to update:
# 1. JwtSettings__Secret - Generate a secure 32+ character secret
# 2. SENDGRID_API_KEY - Your SendGrid API key
# 3. Twilio__AccountSid - Your Twilio Account SID
# 4. Twilio__AuthToken - Your Twilio Auth Token
# 5. Twilio__FromPhoneNumber - Your Twilio phone number

# To update environment variables:
doctl apps update ${APP_ID} --spec /tmp/${APP_NAME}-spec.yaml

# To view deployment logs:
doctl apps logs ${APP_ID}

# To get deployment status:
doctl apps get ${APP_ID}

# To access the database:
doctl databases connection ${APP_ID} --format DSN
EOF

echo -e "${GREEN}Customer environment file created: ${CUSTOMER_NAME}-env.txt${NC}"

# Instructions
echo -e "\n${BLUE}=== NEXT STEPS ===${NC}"
echo "1. Update the environment variables in the app spec:"
echo "   - JWT Secret (generate a secure 32+ character string)"
echo "   - SendGrid API Key"
echo "   - Twilio credentials"
echo ""
echo "2. Update environment variables:"
echo "   doctl apps update ${APP_ID} --spec /tmp/${APP_NAME}-spec.yaml"
echo ""
echo "3. Monitor deployment:"
echo "   doctl apps logs ${APP_ID} --follow"
echo ""
echo "4. Access your app:"
echo "   API: https://${APP_NAME}.ondigitalocean.app"
echo "   Frontend: https://${APP_NAME}-frontend.ondigitalocean.app"
echo ""
echo "5. Set up custom domain (optional):"
echo "   doctl apps update ${APP_ID} --spec domain-spec.yaml"

# Clean up temp file
rm -f /tmp/${APP_NAME}-spec.yaml

echo -e "\n${GREEN}Deployment initiated successfully!${NC}"