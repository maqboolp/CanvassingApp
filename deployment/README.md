# Deployment Scripts

This directory contains scripts for deploying the Hoover Canvassing App for multiple customers.

## Scripts

### setup-customer.sh
Sets up a new customer with proper directory structure and configuration templates.

**Usage:**
```bash
./setup-customer.sh <customer-id> <customer-name> <organization>
```

**Example:**
```bash
./setup-customer.sh johndoe "John Doe" "John Doe for City Council"
```

### deploy-new-customer.sh
Deploys a new instance of the application for a specific customer on DigitalOcean.

**Usage:**
```bash
./deploy-new-customer.sh <customer-name>
```

**Environment Variables:**
- `GITHUB_REPO`: GitHub repository (default: maqboolp/CanvassingApp)
- `GITHUB_BRANCH`: Branch to deploy (default: main)
- `REGION`: DigitalOcean region (default: nyc)

## Workflow

1. **Setup New Customer**:
   ```bash
   ./setup-customer.sh cindymyrex "Cindy Myrex" "Cindy Myrex for Alabama House"
   ```

2. **Configure Customer**:
   - Add logo to `customers/cindymyrex/assets/logo.png`
   - Edit `customers/cindymyrex/config/deployment.env.local`

3. **Deploy**:
   ```bash
   cd customers/cindymyrex/scripts
   ./deploy.sh
   ```

## Requirements

- **doctl**: DigitalOcean CLI tool
- **Authentication**: Run `doctl auth init` first
- **GitHub Access**: Repository must be accessible

## Files Created

When you run `setup-customer.sh`, it creates:

```
customers/<customer-id>/
├── assets/
│   └── README.md
├── config/
│   ├── customer.json
│   └── deployment.env
├── scripts/
│   └── deploy.sh
└── README.md
```

## Configuration

Each customer needs these environment variables configured:

- **Email**: `SENDGRID_API_KEY`
- **SMS/Voice**: `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN`, `TWILIO_FROM_PHONE`
- **Geocoding**: `GOOGLE_GEOCODING_API_KEY`
- **Security**: `JWT_SECRET` (32+ characters)

## Notes

- Customer IDs should be lowercase, no spaces
- Each deployment creates a separate database
- Apps are named: `canvassing-<customer-id>`
- Default region is NYC, change with `REGION` env var

## Troubleshooting

If deployment fails:
1. Check `doctl auth list` for authentication
2. Verify GitHub repository access
3. Ensure customer name is valid (lowercase, no special chars)
4. Review logs: `doctl apps logs <app-id>`

For more details, see [Multi-Tenant Setup Guide](../docs/MULTI_TENANT_SETUP.md).