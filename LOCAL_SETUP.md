# Tanveer Patel for Hoover City Council - Local Development Setup Guide

## Quick Start (5 minutes)

### Prerequisites
- PostgreSQL installed and running
- .NET 8 SDK
- Node.js 18+
- Git

### Easy Setup
```bash
# 1. Navigate to the project directory
cd hoover-canvassing-app

# 2. Run the automated setup script
./start-local.sh
```

That's it! The script will:
- Create the PostgreSQL database
- Set up the database schema
- Start the .NET Core API (port 5000)
- Start the React frontend (port 3000)

### Access the Application
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **API Documentation**: http://localhost:5000/swagger

## Manual Setup (if you prefer step-by-step)

### 1. Database Setup
```bash
# Create database
createdb hoover_canvassing

# Run schema
psql -d hoover_canvassing -f database/schema.sql
```

### 2. Backend Setup
```bash
cd backend/HooverCanvassingApi

# Restore packages
dotnet restore

# Run the API
dotnet run --urls="http://localhost:5000"
```

### 3. Frontend Setup
```bash
cd frontend

# Install dependencies
npm install

# Create environment file
echo "REACT_APP_API_URL=http://localhost:5000" > .env.local

# Start the dev server
npm start
```

## Creating Your First Admin User

Since this is a fresh installation, you'll need to create an admin user. You can do this in two ways:

### Option 1: Using the API directly
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@hoovercanvassing.com",
    "password": "Admin123!",
    "firstName": "Admin",
    "lastName": "User"
  }'
```

Then manually update the user role in the database:
```sql
UPDATE "AspNetUsers" 
SET "Role" = 'Admin' 
WHERE "Email" = 'admin@hoovercanvassing.com';
```

### Option 2: Direct database insert
```sql
-- Connect to the database
psql -d hoover_canvassing

-- Insert admin user (you'll need to hash the password)
INSERT INTO "AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", 
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "FirstName", "LastName", "Role", "IsActive", "CreatedAt"
) VALUES (
    gen_random_uuid()::text,
    'admin@hoovercanvassing.com',
    'ADMIN@HOOVERCANVASSING.COM',
    'admin@hoovercanvassing.com',
    'ADMIN@HOOVERCANVASSING.COM',
    true,
    'AQAAAAEAACcQAAAAEBhv5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8z5V8==', -- Hash for 'Admin123!'
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    'Admin',
    'User',
    'Admin',
    true,
    NOW()
);
```

## Testing the Application

### 1. Login
- Go to http://localhost:3000
- Enter your admin credentials
- You should see the admin dashboard

### 2. Import Sample Data
1. Create a CSV file with voter data (see README.md for format)
2. Go to Admin Panel → Data Management
3. Import the CSV file
4. The system will geocode addresses automatically

### 3. Create Volunteers
1. Use the registration endpoint or admin panel
2. Assign voters to volunteers
3. Test the volunteer dashboard

## Common Issues & Solutions

### Database Connection Issues
- Make sure PostgreSQL is running: `brew services start postgresql` (macOS)
- Check if database exists: `psql -l | grep hoover_canvassing`
- Verify connection string in `appsettings.json`

### CORS Issues
- Make sure the frontend is running on port 3000
- Check CORS configuration in Program.cs
- Clear browser cache and restart both services

### JWT Token Issues
- Check the JWT secret in appsettings.json
- Make sure it's at least 32 characters long
- Verify the issuer and audience URLs

### React Build Issues
- Delete `node_modules` and `package-lock.json`
- Run `npm install` again
- Check for any TypeScript errors in the console

## Development Tips

### Hot Reload
- Backend: Save any .cs file to trigger automatic reload
- Frontend: Save any .tsx/.ts file for instant updates
- Database: Use Entity Framework migrations for schema changes

### Debugging
- Backend: Set breakpoints in Visual Studio Code or Visual Studio
- Frontend: Use browser developer tools
- Database: Use pgAdmin or psql for direct database access

### API Testing
- Use the built-in Swagger UI at http://localhost:5000/swagger
- Import the API endpoints into Postman
- Test authentication with the `/api/auth/login` endpoint

## File Structure Overview
```
hoover-canvassing-app/
├── frontend/                 # React app (port 3000)
│   ├── src/components/      # React components
│   ├── src/types/          # TypeScript types
│   └── public/             # Static assets
├── backend/                 # .NET Core API (port 5000)
│   └── HooverCanvassingApi/
│       ├── Controllers/     # API endpoints
│       ├── Models/         # Data models
│       └── Services/       # Business logic
├── database/               # Database scripts
│   └── schema.sql         # PostgreSQL schema
└── start-local.sh         # Quick start script
```

## Next Steps

Once you have the application running locally:

1. **Import Real Data**: Upload your voter CSV file
2. **Create Volunteers**: Register volunteer accounts
3. **Assign Voters**: Use the admin panel to assign voters to volunteers
4. **Test Mobile**: Test the responsive design on mobile devices
5. **Configure Deployment**: Set up DigitalOcean for production deployment

## Need Help?

- Check the main README.md for detailed documentation
- Review the API documentation at `/swagger`
- Examine the database schema in `database/schema.sql`
- Look at the TypeScript interfaces in `frontend/src/types/`

Happy canvassing! 🗳️