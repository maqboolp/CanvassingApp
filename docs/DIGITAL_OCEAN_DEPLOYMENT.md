# Digital Ocean App Platform Deployment Guide

## Environment Variables Setup

### Required Environment Variables

#### System Configuration
- `SYSTEM_ADMIN_EMAIL` - Email for the system admin account (e.g., admin@yourcampaign.com)
- `EMAIL_FROM_ADDRESS` - Sender email for notifications (e.g., noreply@yourcampaign.com)

#### Database Configuration
- `DATABASE_URL` - PostgreSQL connection string (provided by DO Managed Database)
  - Format: `postgresql://username:password@host:port/database?sslmode=require`

#### Security
- `JWT_SECRET` - Secret key for JWT tokens (generate a strong random string)
- `JWT_ISSUER` - Your app URL (e.g., https://your-app.ondigitalocean.app)
- `JWT_AUDIENCE` - Your app URL (e.g., https://your-app.ondigitalocean.app)

#### Email Service (Choose one)
- **SendGrid**:
  - `SENDGRID_API_KEY` - Your SendGrid API key
- **SMTP**:
  - `SMTP_HOST` - SMTP server host
  - `SMTP_PORT` - SMTP server port
  - `SMTP_USERNAME` - SMTP username
  - `SMTP_PASSWORD` - SMTP password

#### Application Settings
- `ASPNETCORE_ENVIRONMENT` - Set to "Production"
- `ASPNETCORE_URLS` - Set to "http://+:5000" for the backend

### Setting Variables in Digital Ocean

1. **Via Dashboard**:
   - Go to your app → Settings → Environment Variables
   - Click "Edit" or "+ Add Variable"
   - Add each variable with appropriate values
   - Mark sensitive values (API keys, passwords) as "Encrypt"

2. **Via App Spec**:
   - Edit your `app.yaml` file
   - Add variables under the `envs` section
   - Use `type: SECRET` for sensitive values

3. **Via CLI**:
   ```bash
   doctl apps update YOUR_APP_ID --env "SYSTEM_ADMIN_EMAIL=admin@yourcampaign.com"
   ```

### Order of Precedence

The application checks for configuration in this order:
1. **AppSettings table** (database) - Highest priority
2. **Environment variables** - Medium priority  
3. **Default values** - Lowest priority

This means you can:
- Set initial values via Digital Ocean environment variables
- Override them later through the Settings UI without redeployment
- Have defaults as fallback

### First Deployment Steps

1. Set all required environment variables in Digital Ocean
2. Deploy the application
3. The system will automatically:
   - Create the database schema
   - Create the system admin account using `SYSTEM_ADMIN_EMAIL`
   - Use default password: `SystemAdmin@2024!`
4. Login and immediately change the password
5. Navigate to Settings to configure additional options

### Migrating Environment Variables to Database

After deployment, you can migrate all environment variables to the database:

1. Login as SuperAdmin
2. Go to Settings tab
3. Click "Import from Environment"
4. Review and save the imported settings
5. Future changes can be made through the UI without redeployment

### Security Best Practices

1. **Use Digital Ocean's secret management**:
   - Mark sensitive values as encrypted
   - Use different values for staging/production

2. **Rotate secrets regularly**:
   - JWT_SECRET should be rotated periodically
   - Update API keys when needed

3. **Database Security**:
   - Use Digital Ocean Managed Database
   - Enable SSL/TLS connections
   - Set up database backups

4. **Monitor access**:
   - Review login attempts
   - Monitor system admin account usage
   - Set up alerts for suspicious activity