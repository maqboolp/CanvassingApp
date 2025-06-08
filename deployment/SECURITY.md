# Security Configuration for t4h-canvas

## Database Security

The PostgreSQL database is automatically secured by DigitalOcean App Platform:

- ✅ **NOT exposed to the internet** - Database runs in a private network
- ✅ **SSL/TLS encryption** - All connections are encrypted
- ✅ **Firewall protected** - Only accessible from app components
- ✅ **No public IP** - Database has no public endpoint

## Additional Security Measures

### 1. Database Access
- Database is only accessible via the `DATABASE_URL` environment variable
- This URL is automatically injected by DigitalOcean
- Connection string includes SSL mode enforced

### 2. Backup Configuration
After deployment, configure automatic backups:
1. Go to DigitalOcean dashboard
2. Navigate to your database
3. Enable daily backups
4. Set retention period (recommend 7-30 days)

### 3. Environment Variables
Ensure these are set in DigitalOcean dashboard:
- `JWT_SECRET` - Use a strong, random 32+ character string
- `DATABASE_URL` - Automatically set by DigitalOcean
- Never commit secrets to Git

### 4. API Security
- JWT tokens expire after 8 hours
- CORS restricted to your domain only
- All API endpoints require authentication
- Role-based access control (Volunteer, Admin, SuperAdmin)

### 5. Best Practices
- Regularly update dependencies
- Monitor DigitalOcean security alerts
- Use strong passwords for all admin accounts
- Enable 2FA on your DigitalOcean account
- Review access logs periodically

## Monitoring Database Security

To verify database security after deployment:

1. Try to connect directly from your local machine:
   ```bash
   psql postgresql://[connection-string]
   ```
   This should FAIL - database is not publicly accessible

2. Check DigitalOcean dashboard:
   - Database should show "Private Network Only"
   - No public endpoint should be listed

## Emergency Procedures

If you suspect a security breach:
1. Immediately rotate JWT_SECRET in environment variables
2. Force all users to re-login
3. Review database access logs in DigitalOcean
4. Change all admin passwords
5. Contact DigitalOcean support if needed