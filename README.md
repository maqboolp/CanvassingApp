# Political Campaign Canvassing Platform

A multi-tenant web application for political campaign door-to-door canvassing and voter outreach operations. Built with React/TypeScript frontend and .NET Core backend with PostgreSQL database. Deployed on DigitalOcean App Platform.

## 🚀 Features

### Core Functionality
- **Voter Management**: Import and manage voter lists with advanced filtering
- **Contact Tracking**: Log voter interactions with detailed status tracking
- **Route Optimization**: GPS-based nearest voter suggestions and efficient routing
- **Phone Banking**: Integrated calling system with Twilio support
- **SMS Campaigns**: Bulk messaging with opt-in/opt-out compliance
- **Analytics Dashboard**: Real-time campaign metrics and volunteer performance

### Security Features
- **Forced Password Change**: Default admin accounts require password change on first login
- **Password Complexity**: Enforced requirements (uppercase, lowercase, number, special character)
- **JWT Authentication**: Secure token-based authentication with role-based access
- **Database Encryption**: All sensitive data encrypted at rest

### Multi-Tenant Support
- Single codebase supporting multiple campaigns
- Environment-based configuration for each deployment
- Custom branding and logos per campaign
- Isolated databases for data security

## 🛠 Technology Stack

### Frontend
- **React 18** with TypeScript
- **Material-UI (MUI)** for UI components
- **React Router** for navigation
- **Axios** for API communication

### Backend
- **.NET Core 8** Web API
- **Entity Framework Core** with PostgreSQL
- **ASP.NET Core Identity** for authentication
- **JWT Bearer** authentication
- **Twilio** for SMS and voice (optional)
- **SendGrid** for email (optional)

### Infrastructure
- **DigitalOcean App Platform** for hosting
- **DigitalOcean Managed PostgreSQL** for database
- **DigitalOcean Spaces** for file storage (optional)
- **Docker** containerization for backend
- **Static site** deployment for frontend

## 📁 Project Structure

```
hoover-canvassing-app/
├── frontend/                    # React TypeScript application
│   ├── src/
│   │   ├── components/         # React components
│   │   │   └── ChangePassword.tsx  # Forced password change
│   │   ├── services/          # API services
│   │   └── types/             # TypeScript interfaces
│   └── package.json
├── backend/                    # .NET Core Web API
│   └── HooverCanvassingApi/
│       ├── Controllers/        # API controllers
│       ├── Models/            # Entity models
│       ├── Migrations/        # EF Core migrations
│       └── Utils/             # Utilities
├── database/                   # Database files
│   ├── CONSOLIDATED_SCHEMA.sql # Complete database schema
│   └── migrations/            # Migration scripts
├── deployment/                # Deployment configuration
│   ├── DEPLOYMENT_TEMPLATE.yaml    # Master deployment template
│   ├── DEPLOYMENT_GUIDE.md        # Step-by-step guide
│   └── DEFAULT_CREDENTIALS.md     # Security documentation
└── README.md
```

## 🚀 Quick Deployment Guide

### Prerequisites
- DigitalOcean account
- `doctl` CLI tool installed
- GitHub repository access

### Deployment Steps

1. **Create a new branch for your campaign**
```bash
git checkout -b [campaign-name]-app
```

2. **Copy and customize the deployment template**
```bash
cp deployment/DEPLOYMENT_TEMPLATE.yaml [campaign-name]-app.yaml
```

3. **Edit the deployment file** and replace placeholders:
- `${REPLACE_APP_NAME}` → Your app name
- `${REPLACE_DB_NAME}` → Database name
- `${REPLACE_BRANCH}` → Git branch name
- `${REPLACE_JWT_SECRET}` → Generate with `openssl rand -base64 32`
- `${REPLACE_CANDIDATE_NAME}` → Candidate's name
- Other placeholders as needed

4. **Create the database**
```bash
doctl databases db create [cluster-id] [database-name]
```

5. **Apply the database schema**
```bash
PGPASSWORD='[password]' psql -h [host] -p 25060 -U doadmin -d [database-name] < database/CONSOLIDATED_SCHEMA.sql
```

6. **Deploy the application**
```bash
doctl apps create --spec [campaign-name]-app.yaml
```

## 🔐 Default Credentials

After deployment, two admin accounts are created:

### SuperAdmin Account
- **Email**: `superadmin@campaign.com`
- **Password**: `SuperAdmin123!`
- **Force Password Change**: Yes

### Admin Account
- **Email**: `admin@campaign.com`
- **Password**: `Admin123!`
- **Force Password Change**: Yes

⚠️ **IMPORTANT**: These passwords MUST be changed on first login!

## 🔒 Password Requirements

- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

## 📊 Environment Variables

### Required
- `ConnectionStrings__DefaultConnection` - Database connection
- `JwtSettings__Secret` - JWT signing key
- `CAMPAIGN__CANDIDATENAME` - Candidate name
- `CAMPAIGN__CAMPAIGNNAME` - Campaign name

### Optional Services
- `SENDGRID_API_KEY` - Email service
- `TWILIO__ACCOUNTSID` - SMS/Voice service
- `AWS__S3__AccessKey` - File storage
- `GOOGLE_GEOCODING_API_KEY` - Address geocoding

See `deployment/DEPLOYMENT_TEMPLATE.yaml` for complete list.

## 🔄 Database Schema

The consolidated schema includes:

### Core Tables
- `AspNetUsers` - User accounts with Identity
- `Voters` - Voter information
- `Contacts` - Interaction logs
- `Campaigns` - Campaign management

### Feature Tables
- `CampaignMessages` - SMS/Call tracking
- `VoterTags` - Voter categorization
- `ConsentRecords` - SMS opt-in/opt-out
- `PhoneContacts` - Phone banking

## 🛡 Security Best Practices

1. **Change default passwords immediately**
2. **Use strong JWT secrets** (32+ characters)
3. **Enable HTTPS only**
4. **Keep dependencies updated**
5. **Regularly audit user accounts**
6. **Use environment variables for secrets**
7. **Never commit credentials to Git**

## 📈 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - Register new user
- `POST /api/auth/change-password` - Change password

### Voters
- `GET /api/voters` - List voters with filters
- `GET /api/voters/{id}` - Get voter details
- `POST /api/voters/import` - Import CSV

### Campaigns
- `GET /api/campaigns` - List campaigns
- `POST /api/campaigns` - Create campaign
- `POST /api/campaigns/{id}/send-sms` - Send SMS

### Analytics
- `GET /api/analytics/dashboard` - Dashboard metrics
- `GET /api/analytics/volunteer-activity` - Volunteer stats

## 💰 Cost Estimation

### DigitalOcean (Monthly)
- **App Platform**: $12-30/month
- **Managed Database**: $15/month (1GB RAM)
- **Spaces** (optional): $5/month
- **Total**: ~$27-50/month per campaign

## 🔧 Local Development

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- PostgreSQL 15+
- Docker (optional)

### Setup
```bash
# Clone repository
git clone [repository-url]
cd hoover-canvassing-app

# Backend
cd backend/HooverCanvassingApi
dotnet restore
dotnet run

# Frontend
cd frontend
npm install
npm start
```

## 📚 Documentation

- [Deployment Guide](deployment/DEPLOYMENT_GUIDE.md)
- [Deployment Template](deployment/DEPLOYMENT_TEMPLATE.yaml)

## 🤝 Support

For deployment issues or questions:
1. Check the deployment guide
2. Review application logs in DigitalOcean
3. Check GitHub issues
4. Contact the development team

## 📄 License

Proprietary software for political campaign use.

---

**Current Deployments**: Supporting multiple political campaigns with secure, scalable voter outreach tools.