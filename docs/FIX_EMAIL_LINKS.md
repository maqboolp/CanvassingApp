# Fix for Broken Email Links

## Problem
The "Log in to Campaign Portal" and "Review in Admin Dashboard" links in email notifications are not working because they're using localhost URLs or missing the proper frontend URL configuration.

## Root Cause
The `Frontend__BaseUrl` environment variable is not set in production, causing email links to be malformed.

## Solution

### 1. For Production Deployment
Add the following environment variable to your production deployment:

```bash
FRONTEND_URL=https://your-actual-frontend-url.com
```

For example, if your app is hosted at `https://t4happ.com`, set:
```bash
FRONTEND_URL=https://t4happ.com
```

Note: The system supports both `FRONTEND_URL` (preferred) and `Frontend__BaseUrl` environment variables.

### 2. For DigitalOcean App Platform
1. Go to your App's Settings in DigitalOcean
2. Navigate to the Environment Variables section
3. Add the following variable:
   - Key: `FRONTEND_URL`
   - Value: Your frontend URL (e.g., `https://your-app.ondigitalocean.app`)

### 3. For Docker Deployment
Add to your docker-compose.yml or .env file:
```yaml
environment:
  - FRONTEND_URL=https://your-domain.com
```

### 4. For Local Development
The default configuration in `appsettings.json` already has:
```json
"Frontend": {
  "BaseUrl": "http://localhost:3000"
}
```

## Email Links Affected
This configuration affects the following email links:

1. **Registration Approval Email** - "Log in to Campaign Portal" button
   - Links to: `{Frontend__BaseUrl}/login`

2. **Admin Notification Email** - "Review in Admin Dashboard" button
   - Links to: `{Frontend__BaseUrl}/admin/pending-volunteers`

3. **Password Reset Email** - Uses the reset URL passed by the controller

## Testing
After setting the environment variable:
1. Restart your backend application
2. Trigger a test registration to generate an approval email
3. Verify that the links in the email point to your actual domain, not localhost

## Additional Notes
- This setting is separate from `REACT_APP_API_URL` which is used by the frontend
- The `FRONTEND_URL` is used by the backend for:
  - Generating email links
  - CORS configuration
  - JWT issuer/audience (if not explicitly set)
- Make sure to include the protocol (https://) but not a trailing slash
- The system will fall back to `Frontend:BaseUrl` from appsettings.json if `FRONTEND_URL` is not set