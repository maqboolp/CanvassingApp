# Canvassing App Deployment Guide

## Quick Start - Deploy a New Campaign App

### Prerequisites
- DigitalOcean account with API access
- `doctl` CLI installed and configured
- Access to the GitHub repository: `maqboolp/CanvassingApp`

### Step-by-Step Deployment

#### 1. Create a New Branch
```bash
git checkout -b [candidate-name]-app
# Example: git checkout -b robin-litaker-app
```

#### 2. Copy and Configure the Template
```bash
cp deployment/DEPLOYMENT_TEMPLATE.yaml [candidate-name]-app.yaml
# Example: cp deployment/DEPLOYMENT_TEMPLATE.yaml robin-litaker-app.yaml
```

#### 3. Edit the Configuration File
Replace all placeholders in the YAML file:

| Placeholder | Example Value | Description |
|------------|---------------|-------------|
| `${REPLACE_APP_NAME}` | `robin-litaker` | App identifier (lowercase, hyphens) |
| `${REPLACE_DB_NAME}` | `Robin_Litaker` | Database name (use underscores) |
| `${REPLACE_BRANCH}` | `robin-litaker-app` | Git branch name |
| `${REPLACE_JWT_SECRET}` | (generate 32+ chars) | Authentication secret |
| `${REPLACE_CANDIDATE_NAME}` | `Robin Litaker` | Candidate's full name |
| `${REPLACE_CAMPAIGN_NAME}` | `Robin for Alabama` | Campaign slogan |
| `${REPLACE_CAMPAIGN_EMAIL}` | `info@robinlitaker.com` | Campaign email |
| `${REPLACE_OFFICE}` | `Alabama House District 12` | Office seeking |
| `${REPLACE_JURISDICTION}` | `Alabama` | State/jurisdiction |
| `${REPLACE_LOGO_FILE}` | `robin-logo.png` | Logo filename |

#### 4. Add Campaign Logo (Optional)
```bash
# Copy logo to frontend/public/
cp /path/to/logo.png frontend/public/[candidate-name]-logo.png
```

#### 5. Create Database
```bash
# Get the database cluster ID
doctl databases list

# Create new database (using t4h-db cluster)
doctl databases db create c5e5ba69-caea-4ca3-8266-002313cd89ca [DB_NAME]
# Example: doctl databases db create c5e5ba69-caea-4ca3-8266-002313cd89ca Robin_Litaker
```

#### 6. Commit and Push Changes
```bash
git add [candidate-name]-app.yaml
git add frontend/public/[candidate-name]-logo.png  # if added
git commit -m "Add [candidate name] campaign deployment configuration"
git push origin [candidate-name]-app
```

#### 7. Deploy the App
```bash
doctl apps create --spec [candidate-name]-app.yaml
```

#### 8. Monitor Deployment
```bash
# Get app ID from create command output
doctl apps list | grep [app-name]

# Watch logs
doctl apps logs [app-id] --follow

# Check deployment status
doctl apps list-deployments [app-id]
```

#### 9. Get App URL
```bash
doctl apps get [app-id] -o json | jq -r '.[0].live_url'
```

## Default Login Credentials

After deployment, use these credentials:
- **Email**: `superadmin@campaign.local`
- **Password**: `SuperAdmin123`

**Important**: Change these credentials after first login!

## App Architecture

The deployment creates:
1. **API Service**: .NET Core backend using Docker (`/Dockerfile`)
2. **Frontend**: React static site using npm build command (no Docker needed)
3. **Database**: PostgreSQL database in shared cluster
4. **Ingress**: Routes `/api` to backend, `/` to frontend

Note: Frontend uses `build_command: npm run build` instead of Docker for simpler deployment.

## Environment Variables

### Required Variables
- Database connection
- JWT authentication settings
- Email configuration
- Campaign information

### Optional Services
- **SendGrid**: Email service (uncomment in template)
- **Twilio**: SMS/Voice service (uncomment in template)
- **DigitalOcean Spaces**: File storage (uncomment in template)

## Updating an Existing App

### Update Configuration
```bash
doctl apps update [app-id] --spec [candidate-name]-app.yaml
```

### Trigger Manual Deployment
```bash
doctl apps create-deployment [app-id]
```

## Common Issues and Solutions

### Issue: Login Fails
**Solution**: Ensure database exists and connection string is correct

### Issue: API Returns 404
**Solution**: Check ingress rules have `preserve_path_prefix: true`

### Issue: Frontend Can't Connect to API
**Solution**: Verify `REACT_APP_API_URL` is set to `${APP_URL}` (not `/api`)

### Issue: Build Fails
**Solution**: Check branch name exists and has latest code

## File Structure

```
/
├── deployment/
│   ├── DEPLOYMENT_TEMPLATE.yaml  # Master template
│   └── DEPLOYMENT_GUIDE.md        # This guide
├── [candidate-name]-app.yaml     # Individual app configs
├── Dockerfile                     # Backend Docker config
├── backend/                       # .NET API source
└── frontend/                      # React app source
    └── public/
        └── [logo-files].png       # Campaign logos
```

## Cleanup Old Files

The following files are deprecated and can be removed:
- app-spec-corrected.yaml
- app-spec-with-frontend.yaml
- app-spec.example.yaml
- kristin-williams-new-app.yaml
- .do-app-spec-*.yaml files in deployment/

## Support

For issues or questions:
1. Check deployment logs: `doctl apps logs [app-id]`
2. Review app status: `doctl apps get [app-id]`
3. Verify database connection: `doctl databases db list [cluster-id]`