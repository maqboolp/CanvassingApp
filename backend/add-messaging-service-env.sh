#!/bin/bash

# Script to add Twilio Messaging Service SID to app environment variables

APP_ID="4210df4e-200d-4397-82d5-c3157127f965"
MESSAGING_SERVICE_SID="MGd5c85628b8985e7bcc6d588b8c1548d1"

echo "Fetching current app spec..."
doctl apps spec get $APP_ID > current-app-spec.yaml

echo "Current app spec saved to current-app-spec.yaml"
echo ""
echo "IMPORTANT: You need to manually add this environment variable to the spec file:"
echo ""
echo "Find the 'envs:' section in your service/job and add:"
echo "- key: TWILIO__MESSAGINGSERVICESID"
echo "  value: $MESSAGING_SERVICE_SID"
echo "  scope: RUN_TIME"
echo "  type: GENERAL"
echo ""
echo "After editing, run:"
echo "doctl apps update $APP_ID --spec current-app-spec.yaml"
echo ""
echo "Or use the DigitalOcean dashboard to add the environment variable."