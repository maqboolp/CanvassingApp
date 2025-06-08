#!/bin/bash

# Hoover Canvassing App Deployment Script for DigitalOcean
# This script helps deploy the application to DigitalOcean App Platform

set -e

echo "🚀 Starting deployment to DigitalOcean App Platform..."

# Check if doctl is installed
if ! command -v doctl &> /dev/null; then
    echo "❌ doctl CLI not found. Please install it first:"
    echo "   brew install doctl"
    echo "   Or visit: https://github.com/digitalocean/doctl"
    exit 1
fi

# Check if user is authenticated
if ! doctl auth list | grep -q "current"; then
    echo "❌ Please authenticate with DigitalOcean first:"
    echo "   doctl auth init"
    exit 1
fi

# Set variables
APP_NAME="hoover-canvassing-app"
REGION="nyc1"
DB_NAME="hoover-canvassing-db"

echo "📋 Configuration:"
echo "   App Name: $APP_NAME"
echo "   Region: $REGION"
echo "   Database: $DB_NAME"

# Check if app already exists
if doctl apps list | grep -q "$APP_NAME"; then
    echo "📱 App '$APP_NAME' already exists. Updating..."
    
    # Get app ID
    APP_ID=$(doctl apps list --format ID,Spec.Name --no-header | grep "$APP_NAME" | awk '{print $1}')
    
    if [ -z "$APP_ID" ]; then
        echo "❌ Could not find app ID"
        exit 1
    fi
    
    echo "🔄 Updating app (ID: $APP_ID)..."
    doctl apps update "$APP_ID" --spec .do-app-spec.yaml
    
else
    echo "🆕 Creating new app '$APP_NAME'..."
    doctl apps create --spec .do-app-spec.yaml
fi

echo "✅ Deployment initiated successfully!"
echo ""
echo "📊 You can monitor the deployment progress with:"
echo "   doctl apps list"
echo "   doctl apps get <app-id>"
echo ""
echo "🌐 Once deployed, your app will be available at:"
echo "   https://$APP_NAME.ondigitalocean.app"
echo ""
echo "📝 Next steps:"
echo "1. Wait for deployment to complete (usually 5-10 minutes)"
echo "2. Set up environment variables in DigitalOcean dashboard"
echo "3. Import voter CSV data via admin panel"
echo "4. Create volunteer accounts"
echo "5. Assign voters to volunteers"

# Wait for deployment if requested
read -p "🕐 Would you like to wait and monitor the deployment? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "⏳ Monitoring deployment..."
    
    # Get app ID if we just created it
    if [ -z "$APP_ID" ]; then
        sleep 5
        APP_ID=$(doctl apps list --format ID,Spec.Name --no-header | grep "$APP_NAME" | awk '{print $1}')
    fi
    
    # Monitor deployment
    while true; do
        STATUS=$(doctl apps get "$APP_ID" --format ActiveDeployment.Phase --no-header)
        echo "📊 Deployment status: $STATUS"
        
        if [ "$STATUS" = "ACTIVE" ]; then
            echo "✅ Deployment completed successfully!"
            APP_URL=$(doctl apps get "$APP_ID" --format LiveURL --no-header)
            echo "🌐 Your app is live at: $APP_URL"
            break
        elif [ "$STATUS" = "ERROR" ] || [ "$STATUS" = "FAILED" ]; then
            echo "❌ Deployment failed. Check the DigitalOcean dashboard for details."
            exit 1
        fi
        
        sleep 30
    done
fi

echo ""
echo "🎉 Deployment process completed!"
echo "📖 For more information, visit the DigitalOcean dashboard."