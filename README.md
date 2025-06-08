# Tanveer Patel for Hoover City Council - Canvassing App

A comprehensive web and mobile application for supporting Tanveer Patel's Hoover City Council campaign door-to-door canvassing operations. Built with React/TypeScript frontend and .NET Core backend with PostgreSQL database.

## Features

### Core Functionality
- **Voter List Management**: Paginated, filterable voter lists with ZIP code, vote frequency, and age group filters
- **Contact Tracking**: Log voter interactions with status tracking (Reached, Not Home, Refused, Needs Follow-Up)
- **Route Optimization**: GPS-based nearest voter suggestions and route planning
- **Authentication**: Secure login with JWT tokens and role-based access control
- **Admin Analytics**: Comprehensive dashboard with contact metrics and volunteer activity tracking
- **CSV Import**: Bulk voter import with automatic geocoding using OpenStreetMap

### Technical Features
- Responsive design optimized for mobile devices (iPhone, Android, desktop)
- Real-time geolocation integration
- Material-UI components for professional interface
- Secure data encryption and privacy compliance
- Rate-limited API endpoints with comprehensive error handling
- Database indexing for optimal performance with 20,000+ voter records

## Technology Stack

### Frontend
- **React 18** with TypeScript
- **Material-UI (MUI)** for UI components
- **Leaflet.js** for mapping and geolocation
- **Axios** for API communication
- **React Router** for navigation

### Backend
- **.NET Core 8** Web API
- **Entity Framework Core** with PostgreSQL
- **ASP.NET Core Identity** for authentication
- **JWT Bearer** authentication
- **CsvHelper** for data import
- **OpenStreetMap/Nominatim** for geocoding

### Database
- **PostgreSQL 15** with optimized indexes
- Support for spatial queries and geolocation data
- Comprehensive foreign key relationships
- Performance optimization for large datasets

### Infrastructure
- **DigitalOcean App Platform** for hosting
- **DigitalOcean Managed PostgreSQL** for database
- **Docker** containerization
- **HTTPS** encryption and security headers

## Project Structure

```
hoover-canvassing-app/
├── frontend/                 # React TypeScript application
│   ├── src/
│   │   ├── components/      # React components
│   │   ├── types/          # TypeScript interfaces
│   │   └── ...
│   ├── package.json
│   └── tsconfig.json
├── backend/                 # .NET Core Web API
│   └── HooverCanvassingApi/
│       ├── Controllers/     # API controllers
│       ├── Models/         # Entity models
│       ├── Data/           # Database context
│       ├── Services/       # Business logic
│       └── HooverCanvassingApi.csproj
├── database/               # Database scripts
│   └── schema.sql         # PostgreSQL schema
├── deployment/            # Deployment configurations
│   ├── Dockerfile        # Multi-stage Docker build
│   ├── .do-app-spec.yaml # DigitalOcean App Platform spec
│   ├── docker-compose.yml # Local development
│   └── deploy.sh         # Deployment script
└── README.md
```

## Quick Start

### Prerequisites
- Node.js 18+ and npm
- .NET 8 SDK
- PostgreSQL 15+
- Git

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd hoover-canvassing-app
   ```

2. **Set up the database**
   ```bash
   # Create PostgreSQL database
   createdb hoover_canvassing
   
   # Run schema script
   psql -d hoover_canvassing -f database/schema.sql
   ```

3. **Configure backend**
   ```bash
   cd backend/HooverCanvassingApi
   
   # Create appsettings.Development.json
   cat > appsettings.Development.json << EOF
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=hoover_canvassing;Username=postgres;Password=postgres"
     },
     "JwtSettings": {
       "Secret": "your-super-secret-jwt-key-change-in-production-must-be-at-least-32-characters",
       "Issuer": "http://localhost:5000",
       "Audience": "http://localhost:5000",
       "ExpirationMinutes": 480
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   EOF
   
   # Restore packages and run
   dotnet restore
   dotnet run
   ```

4. **Set up frontend**
   ```bash
   cd frontend
   
   # Install dependencies
   npm install
   
   # Create .env.local
   echo "REACT_APP_API_URL=http://localhost:5000" > .env.local
   
   # Start development server
   npm start
   ```

5. **Access the application**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger

### Using Docker Compose

```bash
# Copy environment variables template
cp deployment/.env.example deployment/.env

# Edit environment variables
nano deployment/.env

# Start all services
docker-compose -f deployment/docker-compose.yml up -d

# View logs
docker-compose -f deployment/docker-compose.yml logs -f
```

## Deployment to DigitalOcean

### Prerequisites
- DigitalOcean account
- `doctl` CLI tool installed and configured
- GitHub repository with your code

### Deployment Steps

1. **Install and configure doctl**
   ```bash
   # Install doctl (macOS)
   brew install doctl
   
   # Authenticate
   doctl auth init
   ```

2. **Update deployment configuration**
   ```bash
   # Edit .do-app-spec.yaml to update GitHub repository
   nano deployment/.do-app-spec.yaml
   ```

3. **Deploy using the script**
   ```bash
   cd deployment
   ./deploy.sh
   ```

4. **Set environment variables in DigitalOcean dashboard**
   - `JWT_SECRET`: Generate a secure 32+ character secret
   - `DATABASE_URL`: Will be automatically configured by managed database
   - Any additional configuration as needed

### Manual Deployment

```bash
# Create app
doctl apps create --spec deployment/.do-app-spec.yaml

# Update existing app
doctl apps update <app-id> --spec deployment/.do-app-spec.yaml

# Monitor deployment
doctl apps list
doctl apps get <app-id>
```

## Database Schema

The PostgreSQL database includes the following main tables:

- **AspNetUsers**: Volunteer accounts with ASP.NET Core Identity
- **Voters**: Voter information with geocoded addresses
- **Contacts**: Interaction logs between volunteers and voters
- **VoterAssignments**: Many-to-many relationship for voter assignments

Key indexes are included for optimal performance:
- ZIP code filtering
- Contact status queries
- Geolocation-based searches
- Timeline-based contact retrieval

## API Endpoints

### Authentication
- `POST /api/auth/login` - Volunteer login
- `POST /api/auth/register` - Register new volunteer
- `POST /api/auth/refresh` - Refresh JWT token

### Voters
- `GET /api/voters` - Get paginated voter list with filters
- `GET /api/voters/{id}` - Get specific voter details
- `GET /api/voters/nearest` - Find nearest uncontacted voter

### Contacts
- `POST /api/contacts` - Log new voter contact
- `GET /api/contacts` - Get contact history
- `PUT /api/contacts/{id}` - Update contact information

### Admin
- `POST /api/admin/import-voters` - Import voters from CSV
- `GET /api/admin/analytics` - Get campaign analytics
- `POST /api/admin/assign-voters` - Assign voters to volunteers
- `GET /api/admin/export-analytics` - Export analytics as CSV

## Data Import

### CSV Format

The voter CSV should include the following columns:
- `LALVOTERID`: Unique voter identifier
- `Voters_FirstName`: First name
- `Voters_MiddleName`: Middle name (optional)
- `Voters_LastName`: Last name
- `Residence_Addresses_AddressLine`: Street address
- `Residence_Addresses_City`: City
- `Residence_Addresses_State`: State
- `Residence_Addresses_Zip`: ZIP code
- `Voters_Age`: Age
- `EthnicGroups_EthnicGroup1Desc`: Ethnicity (optional)
- `Voters_Gender`: Gender
- `Vote_Frequency`: Voting frequency pattern
- `VoterTelephones_CellPhoneUnformatted`: Phone number (optional)
- `email`: Email address (optional)

### Import Process

1. **Login as admin** to the web application
2. **Navigate to Admin Panel** → Import Voters
3. **Upload CSV file** with voter data
4. **Enable geocoding** (recommended but optional for performance)
5. **Monitor progress** and review any import errors
6. **Assign voters** to volunteers after successful import

The import process includes:
- Automatic address geocoding using OpenStreetMap
- Data validation and cleanup
- Duplicate detection and skipping
- Error reporting for problematic records
- Rate limiting to respect geocoding service limits

## Security and Privacy

### Data Protection
- All voter data encrypted in transit (HTTPS) and at rest
- JWT token-based authentication with configurable expiration
- Role-based access control (Volunteer vs Admin)
- Input validation and SQL injection prevention
- Rate limiting on API endpoints

### Privacy Compliance
- Secure storage of voter information
- No unauthorized data sharing
- Audit logging of all voter interactions
- Compliance with Alabama data privacy laws
- Option to exclude sensitive voter information

### Recommended Security Practices
- Use strong, unique JWT secrets in production
- Regularly rotate authentication keys
- Monitor access logs for suspicious activity
- Keep dependencies updated for security patches
- Use HTTPS certificates from trusted authorities

## Cost Estimation

### DigitalOcean Pricing (Monthly)
- **App Platform**: $12-30/month (Basic to Professional tier)
- **Managed PostgreSQL**: $15/month (1GB RAM, 10GB storage)
- **Bandwidth**: Included in App Platform tier
- **Total**: ~$27-45/month

### Scaling Considerations
- Basic tier supports 10-20 concurrent volunteers
- Professional tier recommended for 20+ volunteers
- Database can be scaled up as voter data grows
- Consider CDN for improved mobile performance

## Development Guidelines

### Code Style
- TypeScript strict mode enabled
- ESLint and Prettier for code formatting
- C# follows Microsoft coding conventions
- Comprehensive error handling and logging

### Testing
```bash
# Run frontend tests
cd frontend && npm test

# Run backend tests
cd backend/HooverCanvassingApi && dotnet test
```

### Contributing
1. Fork the repository
2. Create feature branch (`git checkout -b feature/your-feature`)
3. Commit changes (`git commit -am 'Add your feature'`)
4. Push to branch (`git push origin feature/your-feature`)
5. Create Pull Request

## Support and Documentation

### Useful Resources
- [DigitalOcean App Platform Docs](https://docs.digitalocean.com/products/app-platform/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [React Documentation](https://reactjs.org/docs/)
- [Material-UI Documentation](https://mui.com/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

### Getting Help
- Check existing GitHub issues
- Review application logs in DigitalOcean dashboard
- Consult API documentation at `/swagger` endpoint
- Contact the development team for campaign-specific questions

### Troubleshooting

#### Common Issues
1. **Database connection errors**: Verify connection string and database availability
2. **Geocoding failures**: Check OpenStreetMap service status and rate limits
3. **Authentication issues**: Verify JWT secret configuration
4. **Performance issues**: Review database indexes and query optimization

#### Logs and Monitoring
- Application logs available in DigitalOcean dashboard
- Database performance metrics in managed database panel
- Frontend errors logged to browser console
- API response times monitored via App Platform metrics

## License

This project is proprietary software developed for Tanveer Patel's Hoover City Council campaign. All rights reserved.

For questions or support, please contact the development team.

---

**Campaign Information**: This application supports voter outreach for Tanveer Patel's August 26, 2025 Hoover City Council nonpartisan election campaign. The platform promotes voter registration and democratic participation in accordance with Alabama election laws.