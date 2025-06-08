# Manual Deployment Guide for t4h-canvas

Since the automated deployment requires GitHub integration, follow these steps to deploy manually:

## Step 1: Connect GitHub to DigitalOcean

1. Go to https://cloud.digitalocean.com/apps
2. Click **"Create App"**
3. Select **"GitHub"** as the source
4. Click **"Manage Access"** and authorize DigitalOcean
5. Grant access to the `maqboolp/CanvassingApp` repository

## Step 2: Create the App

1. Select your repository: `maqboolp/CanvassingApp`
2. Branch: `main`
3. Source Directory: `/` (root)
4. Autodeploy: Enable (recommended)

## Step 3: Configure Resources

### API Service
- **Name**: api
- **Resource Type**: Web Service
- **Build Command**: (leave empty - uses Dockerfile)
- **Run Command**: (leave empty - uses Dockerfile)
- **HTTP Port**: 8080
- **Dockerfile Path**: deployment/Dockerfile
- **Resource Size**: Basic ($5/month)

### Frontend Static Site
- **Name**: frontend
- **Resource Type**: Static Site
- **Build Command**: `npm ci && npm run build`
- **Output Directory**: `build`
- **Source Directory**: `/frontend`

### Database
- **Engine**: PostgreSQL
- **Version**: 15
- **Size**: Dev Database ($7/month)

## Step 4: Environment Variables

Click on the API component and add these environment variables:

```
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET=[GENERATE A 32+ CHARACTER RANDOM STRING]
```

The database connection will be automatically configured.

## Step 5: Deploy

1. Click **"Next"** through the configuration
2. Review the monthly cost (should be ~$12-17/month)
3. Click **"Create Resources"**
4. Wait for deployment (10-15 minutes)

## Step 6: Post-Deployment

1. Your app will be available at: https://t4h-canvas.ondigitalocean.app
2. Create the first SuperAdmin user:
   - Email: superadmin@tanveer4hoover.com
   - Password: (set a strong password)
3. Import voter data via the admin panel
4. Create volunteer accounts

## Monitoring

- View logs: DigitalOcean Dashboard > Apps > t4h-canvas > Runtime Logs
- View metrics: DigitalOcean Dashboard > Apps > t4h-canvas > Insights
- Database backups: Enabled automatically

## Troubleshooting

If deployment fails:
1. Check build logs in DigitalOcean dashboard
2. Verify all environment variables are set
3. Ensure database migration runs (check API logs)
4. Contact DigitalOcean support if needed