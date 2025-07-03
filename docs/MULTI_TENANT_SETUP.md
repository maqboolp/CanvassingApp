# Multi-Tenant Setup Guide

This guide explains how to deploy the Hoover Canvassing App for multiple customers using our multi-tenant architecture.

## Overview

The application supports multiple deployment models:
1. **Separate Deployments** (Recommended): Each customer gets their own isolated deployment
2. **Multi-Tenant**: Single deployment serving multiple customers (future enhancement)

Currently, we use the **Separate Deployments** model for better isolation, security, and customization.

## Directory Structure

```
hoover-canvassing-app/
├── customers/              # Customer-specific configurations
│   ├── cindymyrex/        # Example customer
│   │   ├── assets/        # Logo and branding
│   │   ├── config/        # Configuration files
│   │   └── scripts/       # Deployment scripts
│   └── [customer-id]/     # Your customers
├── deployment/            # Deployment scripts
│   ├── setup-customer.sh  # Customer setup script
│   └── deploy-new-customer.sh
└── docs/                  # Documentation
```

## Quick Start

### 1. Set Up a New Customer

```bash
cd deployment
./setup-customer.sh <customer-id> <customer-name> <organization>

# Example:
./setup-customer.sh johndoe "John Doe" "John Doe for City Council"
```

This creates:
- Customer directory structure
- Configuration templates
- Deployment scripts
- Documentation

### 2. Configure the Customer

1. **Add Logo**:
   ```bash
   # Copy logo to customer assets
   cp /path/to/logo.png customers/johndoe/assets/logo.png
   ```

2. **Configure Environment**:
   ```bash
   cd customers/johndoe/config
   cp deployment.env deployment.env.local
   # Edit deployment.env.local with your values
   ```

3. **Required Configuration**:
   - `SENDGRID_API_KEY`: Your SendGrid API key for emails
   - `TWILIO_ACCOUNT_SID`: Twilio account SID
   - `TWILIO_AUTH_TOKEN`: Twilio auth token
   - `TWILIO_FROM_PHONE`: Twilio phone number (format: +1234567890)
   - `GOOGLE_GEOCODING_API_KEY`: Google Maps API key
   - `JWT_SECRET`: Secure secret (32+ characters)

### 3. Deploy the Application

```bash
cd customers/johndoe/scripts
./deploy.sh
```

Or manually:
```bash
cd deployment
./deploy-new-customer.sh "John Doe"
```

## Customer Configuration

### customer.json Structure

Each customer has a `customer.json` file with:

```json
{
  "customer": {
    "id": "unique-id",
    "name": "Display Name",
    "organization": "Full Organization Name"
  },
  "branding": {
    "logoUrl": "/assets/logo.png",
    "appTitle": "Custom App Title",
    "primaryColor": "#673de6",
    "secondaryColor": "#ffd700"
  },
  "api": {
    "endpoints": {
      "production": "https://api-url",
      "staging": "https://staging-api-url"
    }
  },
  "features": {
    "smsEnabled": true,
    "robocallEnabled": true,
    "emailEnabled": true,
    "geocodingEnabled": true
  }
}
```

### Environment Variables

The application uses environment variables for customer-specific configuration:

**Frontend Variables**:
- `REACT_APP_CUSTOMER_ID`: Customer identifier
- `REACT_APP_LOGO_URL`: Path to customer logo
- `REACT_APP_APP_TITLE`: Application title
- `REACT_APP_API_URL`: API endpoint

**Backend Variables**:
- `ConnectionStrings__DefaultConnection`: Database connection
- `JwtSettings__Secret`: JWT secret key
- `SENDGRID_API_KEY`: SendGrid API key
- `Twilio__*`: Twilio configuration
- `GOOGLE_GEOCODING_API_KEY`: Google Maps API key

## Managing Multiple Customers

### List All Customers

```bash
ls -la customers/
```

### Update a Customer

1. Modify configuration in `customers/<customer-id>/config/`
2. Update environment variables in DigitalOcean:
   ```bash
   doctl apps update <app-id> --spec <spec-file>
   ```

### Remove a Customer

1. Delete the app in DigitalOcean:
   ```bash
   doctl apps delete <app-id>
   ```
2. Remove customer directory:
   ```bash
   rm -rf customers/<customer-id>
   ```

## Best Practices

1. **Unique Customer IDs**: Use lowercase, no spaces (e.g., `johndoe`, `citycouncil2024`)
2. **Secure Secrets**: Generate strong JWT secrets (32+ characters)
3. **Logo Guidelines**: 
   - Format: PNG with transparent background
   - Size: 300x100px recommended
   - Location: `customers/<id>/assets/logo.png`
4. **Backup Configuration**: Keep `deployment.env.local` files secure and backed up
5. **Test First**: Deploy to staging before production

## Database Management

Each customer gets their own database. Options:

1. **Automatic** (Default): DigitalOcean creates a new database
2. **Existing Database**: Specify in `DB_CONNECTION_STRING`
3. **Shared Cluster**: Use same cluster, different databases

## Monitoring and Maintenance

### View Logs
```bash
doctl apps logs <app-id> --follow
```

### Check Status
```bash
doctl apps get <app-id>
```

### Update Deployment
```bash
cd customers/<customer-id>
git pull
./scripts/deploy.sh
```

## Troubleshooting

### Common Issues

1. **Deployment Fails**
   - Check GitHub repo access
   - Verify environment variables
   - Review deployment logs

2. **Logo Not Showing**
   - Ensure logo is at correct path
   - Check file permissions
   - Verify REACT_APP_LOGO_URL

3. **Database Connection Issues**
   - Verify connection string format
   - Check SSL mode settings
   - Ensure database exists

### Debug Commands

```bash
# Check app status
doctl apps get <app-id>

# View recent logs
doctl apps logs <app-id> --tail 100

# List all apps
doctl apps list

# Get database connection string
doctl databases connection <db-id>
```

## Advanced Configuration

### Custom Domains

1. Add domain to DigitalOcean:
   ```bash
   doctl apps update <app-id> --spec domain-spec.yaml
   ```

2. Configure DNS:
   - Point domain to DigitalOcean nameservers
   - Or add CNAME records

### Scaling

Modify instance sizes in deployment spec:
- `basic-xxs`: Development (512MB RAM)
- `basic-xs`: Small production (1GB RAM)
- `basic-s`: Medium production (2GB RAM)

### Backup Strategy

1. **Database Backups**: Enable in DigitalOcean dashboard
2. **Configuration Backup**: Version control `customers/` directory
3. **Asset Backup**: Store logos/assets separately

## Security Considerations

1. **Separate Databases**: Each customer has isolated data
2. **Unique JWT Secrets**: Different per customer
3. **API Key Isolation**: Customer-specific API keys
4. **CORS Configuration**: Restricted to customer domains
5. **SSL/TLS**: Enforced by default

## Support

For issues or questions:
1. Check deployment logs
2. Review this documentation
3. Consult main project README
4. Open an issue on GitHub