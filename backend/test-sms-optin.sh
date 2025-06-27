#!/bin/bash

# Test SMS Opt-In System

# Configuration - Update these values
API_URL="https://t4h-canvas-2uwxt.ondigitalocean.app"
ADMIN_TOKEN="YOUR_JWT_TOKEN"  # Get this by logging in as admin
TEST_PHONE="+1234567890"  # Your test phone number

echo "=== Testing SMS Opt-In System ==="
echo ""

# 1. Test Opt-In Status Check
echo "1. Checking opt-in status for $TEST_PHONE..."
curl -X GET "$API_URL/api/optin/status/$TEST_PHONE" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

echo ""
echo ""

# 2. Test Web Form Opt-In
echo "2. Testing web form opt-in..."
curl -X POST "$API_URL/api/optin/web-form" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "'$TEST_PHONE'",
    "consentGiven": true,
    "firstName": "Test",
    "lastName": "User",
    "zipCode": "35244"
  }'

echo ""
echo ""

# 3. Preview Opt-In Invitation (Admin only)
echo "3. Preview opt-in invitation..."
curl -X POST "$API_URL/api/optininvitation/preview" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "filter": {
      "zipCodes": ["35244"],
      "maxRecipients": 5
    }
  }'

echo ""
echo ""

# 4. Get SMS Stats (Admin only)
echo "4. Getting SMS opt-in statistics..."
curl -X GET "$API_URL/api/optininvitation/stats" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

echo ""
echo ""
echo "=== Test Complete ==="
echo ""
echo "Check your phone for SMS messages!"
echo "You can also check the logs in DigitalOcean to verify Messaging Service is being used."