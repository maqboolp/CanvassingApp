#!/bin/bash

# Configuration for cindymyrex deployment
# Fill in these values before running the update

APP_ID="5103bfe7-8bef-43d9-b9c5-dd29e5c41808"
APP_NAME="canvassing-cindymyrex"

# Generate a secure JWT secret (32+ characters)
# You can generate one with: openssl rand -base64 32
JWT_SECRET="4OlmIkGMuiYjxJGK+Cc5dvwHxCqlu1MvQOiGY9UdKho="

# SendGrid Configuration (for email)
SENDGRID_API_KEY="YOUR_SENDGRID_API_KEY_HERE"

# Twilio Configuration (for SMS)
TWILIO_ACCOUNT_SID="YOUR_TWILIO_ACCOUNT_SID_HERE"
TWILIO_AUTH_TOKEN="YOUR_TWILIO_AUTH_TOKEN_HERE"
TWILIO_PHONE_NUMBER="+1XXXXXXXXXX"  # Format: +1XXXXXXXXXX

# Optional: DigitalOcean Spaces for file storage
DO_SPACES_KEY="YOUR_DO_SPACES_KEY_HERE"
DO_SPACES_SECRET="YOUR_DO_SPACES_SECRET_HERE"
DO_SPACES_ENDPOINT="nyc3.digitaloceanspaces.com"
DO_SPACES_BUCKET="hoover-canvassing-audio"

# Get current app spec first
doctl apps spec get ${APP_ID} > /tmp/${APP_NAME}-current-spec.yaml

# Create the updated spec with just env var changes
cat > /tmp/${APP_NAME}-env-update.yaml <<EOF
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

# Merge the specs using Python
python3 -c "
import yaml
import sys

# Read current spec
with open('/tmp/${APP_NAME}-current-spec.yaml', 'r') as f:
    current = yaml.safe_load(f.read())

# Read env updates
with open('/tmp/${APP_NAME}-env-update.yaml', 'r') as f:
    updates = yaml.safe_load(f.read())

# Update the envs for the api service
for service in current['services']:
    if service['name'] == 'api':
        # Keep existing envs that aren't being updated
        existing_envs = {env['key']: env for env in service.get('envs', [])}
        new_envs = {env['key']: env for env in updates['services'][0]['envs']}
        existing_envs.update(new_envs)
        service['envs'] = list(existing_envs.values())

# Write updated spec
with open('/tmp/${APP_NAME}-update-spec.yaml', 'w') as f:
    yaml.dump(current, f, default_flow_style=False)
"

echo "Configuration ready. To apply these settings, run:"
echo "doctl apps update ${APP_ID} --spec /tmp/${APP_NAME}-update-spec.yaml"
echo ""
echo "To generate a secure JWT secret, run:"
echo "openssl rand -base64 32"
