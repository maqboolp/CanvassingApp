# SuperAdmin Email Configuration

## Overview
The system creates a protected SuperAdmin account on first startup. This account cannot be deleted or deactivated by any user.

## Setting the SuperAdmin Email

### Method 1: Environment Variable (Before First Run)
Set the `SYSTEM_ADMIN_EMAIL` environment variable before running the application for the first time:

```bash
export SYSTEM_ADMIN_EMAIL="admin@yourcampaign.com"
```

### Method 2: Database Setting (After First Run)
1. Navigate to the SuperAdmin dashboard
2. Go to the "Settings" tab
3. Add a new setting:
   - Key: `SYSTEM_ADMIN_EMAIL`
   - Value: `admin@yourcampaign.com`
   - Category: `System`
   - Description: `Email address for the system administrator account`

### Method 3: SQL Script
Run the provided SQL script after database migrations:

```bash
psql -U youruser -d yourdatabase -f backend/scripts/add-system-admin-email-setting.sql
```

## Default Credentials
- **Email**: `systemadmin@hoover.local` (or your configured email)
- **Password**: `SystemAdmin@2024!`

**IMPORTANT**: Change this password immediately after first login!

## Email Configuration for Notifications
The system also needs an email address for sending notifications:

1. Through Settings UI:
   - Key: `EMAIL_FROM_ADDRESS`
   - Value: `noreply@yourcampaign.com`
   - Category: `Email`

2. Through Environment Variable:
   ```bash
   export EMAIL_FROM_ADDRESS="noreply@yourcampaign.com"
   ```

## Security Notes
- The system admin account is protected and cannot be:
  - Deleted by any user
  - Deactivated by any user
  - Have password reset by other admins
- Only the system admin can reset their own password through the forgot password feature
- The account is marked with a special badge in the admin dashboard