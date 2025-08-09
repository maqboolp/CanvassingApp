# Default Credentials and Password Management

## Default Admin Accounts

When a new deployment is created, two default admin accounts are automatically created:

### SuperAdmin Account
- **Email**: superadmin@campaign.com
- **Default Password**: SuperAdmin123!
- **Role**: SuperAdmin (full system access)
- **Force Password Change**: Yes (required on first login)

### Admin Account
- **Email**: admin@campaign.com
- **Default Password**: Admin123!
- **Role**: Admin (campaign management access)
- **Force Password Change**: Yes (required on first login)

## Important Security Notes

⚠️ **CRITICAL**: These default passwords MUST be changed immediately upon first login!

1. **Forced Password Change**: Both default accounts have the `ForcePasswordChange` flag set to `true`. Users will be prompted to change their password on first login.

2. **Password Requirements**:
   - Minimum 8 characters
   - At least one uppercase letter
   - At least one lowercase letter
   - At least one number
   - At least one special character

3. **First Login Process**:
   - Login with default credentials
   - System will return `ForcePasswordChange: true` in the login response
   - Frontend should redirect to password change screen
   - User must provide current password and new password
   - After successful password change, the `ForcePasswordChange` flag is reset

## Frontend Implementation

The frontend should check the `ForcePasswordChange` flag in the login response:

```javascript
// Example login response handling
const loginResponse = await api.login(email, password);

if (loginResponse.data.forcePasswordChange) {
  // Redirect to password change screen
  // Store token temporarily but restrict access to other features
  router.push('/change-password?forced=true');
} else {
  // Normal login flow
  router.push('/dashboard');
}
```

## API Endpoints

### Login Endpoint
`POST /api/auth/login`

Response includes:
```json
{
  "success": true,
  "data": {
    "id": "user-id",
    "email": "admin@campaign.com",
    "firstName": "Campaign",
    "lastName": "Admin",
    "role": "admin",
    "token": "jwt-token",
    "avatarUrl": "gravatar-url",
    "forcePasswordChange": true
  }
}
```

### Change Password Endpoint
`POST /api/auth/change-password`

Request:
```json
{
  "currentPassword": "Admin123!",
  "newPassword": "NewSecurePassword123!"
}
```

## Database Schema

The `AspNetUsers` table includes:
- `ForcePasswordChange` BOOLEAN NOT NULL DEFAULT FALSE

## Deployment Steps

1. Create new database
2. Apply CONSOLIDATED_SCHEMA.sql (includes default users)
3. Deploy application
4. Login with default credentials
5. Change passwords immediately
6. Create campaign-specific admin accounts
7. Optionally disable default accounts after creating new admins

## Security Best Practices

1. **Never share default passwords** in production documentation
2. **Change default passwords immediately** after deployment
3. **Create campaign-specific admin accounts** with unique emails
4. **Consider disabling default accounts** after setup
5. **Use strong, unique passwords** for each deployment
6. **Enable two-factor authentication** when available
7. **Regularly audit user accounts** and remove inactive users

## Troubleshooting

### Cannot Login with Default Credentials
- Ensure database schema was applied correctly
- Check if password hashes are properly stored
- Verify the application can connect to the database

### Password Change Not Working
- Verify current password is correct
- Ensure new password meets requirements
- Check API logs for specific error messages

### ForcePasswordChange Flag Not Resetting
- Check that the user update is being saved to database
- Verify the change-password endpoint includes the flag reset logic