# Database Migration Guide

## Using the Consolidated Schema

Instead of running 25+ individual Entity Framework migrations, use the consolidated schema for new deployments.

### For New Deployments

1. **Create the database**:
```bash
doctl databases db create [cluster-id] [database-name]
# Example: doctl databases db create c5e5ba69-caea-4ca3-8266-002313cd89ca Sarah_Smith
```

2. **Apply the consolidated schema**:
```bash
PGPASSWORD='[password]' psql -h [host] -p 25060 -U doadmin -d [database-name] < database/CONSOLIDATED_SCHEMA.sql
```

### Benefits of Consolidated Schema

✅ **Single Migration**: One file instead of 25+ migration files  
✅ **Faster Setup**: Creates all tables at once  
✅ **No Dependency Issues**: Avoids migration order problems  
✅ **Complete Schema**: Includes all tables, indexes, and relationships  
✅ **Idempotent**: Safe to run multiple times (uses IF NOT EXISTS)  

### What's Included

The consolidated schema creates:

#### Core Tables
- Identity/Authentication tables (AspNetUsers, AspNetRoles, etc.)
- Voters table with all fields including PartyAffiliation
- Contacts table for voter interactions
- Campaigns and CampaignMessages

#### Feature Tables
- InvitationTokens for user invites
- VolunteerResources for training materials
- VoterTags and VoterTagAssignments for categorization
- ConsentRecords for SMS/call consent
- VoiceRecordings for robocalls
- ResourceLinks for quick access links
- TwilioPhoneNumbers and TwilioConfiguration
- PhoneContacts for phone banking

#### Indexes
- All necessary indexes for performance
- Unique constraints where appropriate

### Migration from Existing Database

If you have an existing database with old migrations:

1. **Backup your data** first!
2. Check which migrations are applied:
```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

3. If all tables exist, just add the consolidated migration record:
```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250809000000_ConsolidatedInitialMigration', '8.0.0');
```

### Troubleshooting

#### Error: Column already exists
The schema uses `IF NOT EXISTS` clauses, so this shouldn't happen. If it does, the column might exist with different properties.

#### Error: Table doesn't exist
Make sure you're connected to the right database and have proper permissions.

#### Error: Migration already applied
This is fine - the schema is idempotent and safe to run multiple times.

### For Developers

When adding new tables/columns:
1. Add them to `CONSOLIDATED_SCHEMA.sql`
2. Use `IF NOT EXISTS` for safety
3. Include appropriate indexes
4. Document in this guide

### Old Migrations

The individual migration files in `/backend/HooverCanvassingApi/Migrations/` are kept for reference but should not be used for new deployments. Use the consolidated schema instead.