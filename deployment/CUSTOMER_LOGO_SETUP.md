# Customer Logo Configuration Guide

This guide explains how to configure custom logos and branding for each customer deployment.

## Overview

The application now supports environment-based logos and branding, allowing each customer to have their own logo without modifying the source code.

## Environment Variables

The following environment variables control the logo and branding:

- `REACT_APP_LOGO_URL` - URL to the customer's logo image
- `REACT_APP_LOGO_ALT` - Alt text for the logo (for accessibility)
- `REACT_APP_TITLE` - Application title shown in browser tab

## Setup Instructions

### Option 1: Using a Hosted Logo URL

1. Host the customer's logo on their website or a CDN
2. Update the deployment configuration with the logo URL:

```bash
REACT_APP_LOGO_URL="https://customer-domain.com/logo.png"
REACT_APP_LOGO_ALT="Customer Name Campaign"
REACT_APP_TITLE="Customer Name Canvassing"
```

### Option 2: Using DigitalOcean Spaces

1. Upload the logo to your DigitalOcean Spaces bucket:
```bash
doctl spaces upload logo-cindymyrex.png s3://hoover-canvassing-audio/logos/cindymyrex.png --acl public-read
```

2. Use the Spaces URL in the configuration:
```bash
REACT_APP_LOGO_URL="https://hoover-canvassing-audio.nyc3.digitaloceanspaces.com/logos/cindymyrex.png"
```

### Option 3: Include Logo in Repository

1. Place the logo in `frontend/public/logos/customer-name.png`
2. Reference it with a relative path:
```bash
REACT_APP_LOGO_URL="/logos/customer-name.png"
```

## Deployment Configuration

When deploying for a new customer, update these sections in your deployment script:

```yaml
- name: frontend-static
  envs:
  - key: REACT_APP_LOGO_URL
    value: "https://customer-domain.com/logo.png"
  - key: REACT_APP_LOGO_ALT
    value: "Customer Name Campaign"
  - key: REACT_APP_TITLE
    value: "Customer Name Canvassing"
```

## Logo Requirements

- **Format**: PNG or JPG recommended
- **Dimensions**: Maximum width 180px, height 60px (will be scaled down if larger)
- **File Size**: Keep under 500KB for fast loading
- **Transparency**: PNG with transparent background works best

## Example: Cindy Myrex Configuration

```bash
# In deployment script
REACT_APP_LOGO_URL="https://cindymyrex.com/logo.png"
REACT_APP_LOGO_ALT="Cindy Myrex Campaign"
REACT_APP_TITLE="Cindy Myrex Canvassing"
```

## Updating an Existing Deployment

To update the logo for an already deployed app:

```bash
# Update environment variables
doctl apps update APP_ID --spec - <<EOF
services:
- name: frontend-static
  envs:
  - key: REACT_APP_LOGO_URL
    value: "NEW_LOGO_URL"
  - key: REACT_APP_LOGO_ALT
    value: "NEW_ALT_TEXT"
EOF
```

## Default Fallback

If no environment variables are set, the app will use:
- Logo: `/campaign-logo.png`
- Alt Text: `Campaign Logo`
- Title: `Canvassing App`

## Testing Locally

To test logo configuration locally:

```bash
cd frontend
REACT_APP_LOGO_URL="https://example.com/logo.png" \
REACT_APP_LOGO_ALT="Test Campaign" \
REACT_APP_TITLE="Test Canvassing" \
npm start
```

## Troubleshooting

1. **Logo not showing**: Check that the URL is publicly accessible
2. **CORS errors**: Ensure the logo host allows cross-origin requests
3. **Logo too large**: Resize to recommended dimensions before hosting
4. **Changes not appearing**: Force a new deployment after updating environment variables