-- Reset Database Script for Robert Williams App
-- This script drops all existing tables and allows the application to recreate them with the correct schema

-- Drop all existing tables in the correct order (respecting foreign key constraints)
DROP TABLE IF EXISTS "VoterTagAssignments" CASCADE;
DROP TABLE IF EXISTS "VoterTags" CASCADE;
DROP TABLE IF EXISTS "ConsentRecords" CASCADE;
DROP TABLE IF EXISTS "CampaignMessages" CASCADE;
DROP TABLE IF EXISTS "Campaigns" CASCADE;
DROP TABLE IF EXISTS "PhoneContacts" CASCADE;
DROP TABLE IF EXISTS "Contacts" CASCADE;
DROP TABLE IF EXISTS "VoiceRecordings" CASCADE;
DROP TABLE IF EXISTS "VolunteerResources" CASCADE;
DROP TABLE IF EXISTS "ResourceLinks" CASCADE;
DROP TABLE IF EXISTS "TwilioPhoneNumbers" CASCADE;
DROP TABLE IF EXISTS "TwilioConfigurations" CASCADE;
DROP TABLE IF EXISTS "PendingVolunteers" CASCADE;
DROP TABLE IF EXISTS "InvitationTokens" CASCADE;
DROP TABLE IF EXISTS "Voters" CASCADE;

-- Drop ASP.NET Identity tables
DROP TABLE IF EXISTS "AspNetUserTokens" CASCADE;
DROP TABLE IF EXISTS "AspNetUserLogins" CASCADE;
DROP TABLE IF EXISTS "AspNetUserClaims" CASCADE;
DROP TABLE IF EXISTS "AspNetRoleClaims" CASCADE;
DROP TABLE IF EXISTS "AspNetUserRoles" CASCADE;
DROP TABLE IF EXISTS "AspNetUsers" CASCADE;
DROP TABLE IF EXISTS "AspNetRoles" CASCADE;

-- Drop Entity Framework migrations history table
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- Verify all tables are dropped
SELECT 'Tables dropped successfully. The database is now clean and ready for fresh migrations.' as status;