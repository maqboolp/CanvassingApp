-- Consolidated Database Schema for Hoover Canvassing App
-- This single migration creates all tables and relationships
-- Use this instead of running multiple individual migrations

-- Drop existing tables if needed (be careful in production!)
-- DROP SCHEMA public CASCADE;
-- CREATE SCHEMA public;

-- Create Migration History Table (Entity Framework)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) NOT NULL,
    "ProductVersion" VARCHAR(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Identity Tables
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Role" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastLoginAt" TIMESTAMP WITH TIME ZONE,
    "LastActivity" TIMESTAMP WITH TIME ZONE,
    "LoginCount" INTEGER NOT NULL DEFAULT 0,
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL,
    "TwoFactorEnabled" BOOLEAN NOT NULL,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL NOT NULL,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL NOT NULL,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- Core Application Tables
CREATE TABLE IF NOT EXISTS "Voters" (
    "LalVoterId" TEXT NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "MiddleName" TEXT,
    "LastName" VARCHAR(100) NOT NULL,
    "AddressLine" VARCHAR(500) NOT NULL,
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(50) NOT NULL,
    "Zip" VARCHAR(10) NOT NULL,
    "Age" INTEGER NOT NULL,
    "Ethnicity" TEXT,
    "Gender" VARCHAR(20) NOT NULL,
    "PartyAffiliation" TEXT,
    "VoteFrequency" TEXT NOT NULL,
    "CellPhone" TEXT,
    "Email" TEXT,
    "Latitude" DOUBLE PRECISION,
    "Longitude" DOUBLE PRECISION,
    "IsContacted" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastContactStatus" TEXT,
    "VoterSupport" INTEGER NOT NULL DEFAULT 0,
    "CallCount" INTEGER NOT NULL DEFAULT 0,
    "LastCallAt" TIMESTAMP WITH TIME ZONE,
    "LastCallCampaignId" UUID,
    "LastCampaignContactAt" TIMESTAMP WITH TIME ZONE,
    "LastCampaignId" UUID,
    "LastSmsAt" TIMESTAMP WITH TIME ZONE,
    "LastSmsCampaignId" UUID,
    "SmsCount" INTEGER NOT NULL DEFAULT 0,
    "TotalCampaignContacts" INTEGER NOT NULL DEFAULT 0,
    "Religion" TEXT,
    "Income" TEXT,
    "Notes" TEXT,
    "SupportLevel" INTEGER DEFAULT 0,
    "AssignedVolunteerId" TEXT,
    "ContactedAt" TIMESTAMP WITH TIME ZONE,
    "ContactedByVolunteerId" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "CampaignId" UUID,
    "LastContactDate" TIMESTAMP WITH TIME ZONE,
    "TotalContacts" INTEGER DEFAULT 0,
    CONSTRAINT "PK_Voters" PRIMARY KEY ("LalVoterId")
);

CREATE TABLE IF NOT EXISTS "Campaigns" (
    "Id" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Type" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "StartDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "EndDate" TIMESTAMP WITH TIME ZONE,
    "TargetCount" INTEGER NOT NULL DEFAULT 0,
    "CompletedCount" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "Script" TEXT,
    "IsRobocall" BOOLEAN NOT NULL DEFAULT FALSE,
    "AudioFileUrl" TEXT,
    "MaxCallAttempts" INTEGER NOT NULL DEFAULT 3,
    "ActiveCalls" INTEGER NOT NULL DEFAULT 0,
    "TotalCallsPlaced" INTEGER NOT NULL DEFAULT 0,
    "TotalCallsCompleted" INTEGER NOT NULL DEFAULT 0,
    "LastResumedAt" TIMESTAMP WITH TIME ZONE,
    "IsPaused" BOOLEAN NOT NULL DEFAULT FALSE,
    "UseVoiceRecording" BOOLEAN NOT NULL DEFAULT FALSE,
    "VoiceRecordingId" UUID,
    "CallWindowStart" TIME WITHOUT TIME ZONE,
    "CallWindowEnd" TIME WITHOUT TIME ZONE,
    "EnableWeekends" BOOLEAN NOT NULL DEFAULT FALSE,
    "PreventDuplicateMessages" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "PK_Campaigns" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Contacts" (
    "Id" TEXT NOT NULL,
    "VoterId" TEXT NOT NULL,
    "VolunteerId" TEXT NOT NULL,
    "CampaignId" UUID,
    "Status" TEXT NOT NULL,
    "Notes" TEXT,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LocationLatitude" DOUBLE PRECISION,
    "LocationLongitude" DOUBLE PRECISION,
    "AudioFileUrl" TEXT,
    "PhotoUrl" TEXT,
    CONSTRAINT "PK_Contacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Contacts_AspNetUsers_VolunteerId" FOREIGN KEY ("VolunteerId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Contacts_Campaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "Campaigns" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Contacts_Voters_VoterId" FOREIGN KEY ("VoterId") REFERENCES "Voters" ("LalVoterId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "CampaignMessages" (
    "Id" UUID NOT NULL,
    "CampaignId" UUID NOT NULL,
    "VoterId" TEXT NOT NULL,
    "MessageType" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "MessageContent" TEXT,
    "AttemptCount" INTEGER NOT NULL DEFAULT 0,
    "LastAttemptAt" TIMESTAMP WITH TIME ZONE,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "TwilioSid" TEXT,
    "CallDuration" INTEGER,
    CONSTRAINT "PK_CampaignMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CampaignMessages_Campaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "Campaigns" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CampaignMessages_Voters_VoterId" FOREIGN KEY ("VoterId") REFERENCES "Voters" ("LalVoterId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "InvitationTokens" (
    "Id" UUID NOT NULL,
    "Token" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsUsed" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "UsedAt" TIMESTAMP WITH TIME ZONE,
    "UsedBy" TEXT,
    CONSTRAINT "PK_InvitationTokens" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "VolunteerResources" (
    "Id" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "FileUrl" TEXT,
    "ResourceType" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_VolunteerResources" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "VoterTags" (
    "Id" UUID NOT NULL,
    "Name" VARCHAR(50) NOT NULL,
    "Description" TEXT,
    "Color" VARCHAR(7) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    CONSTRAINT "PK_VoterTags" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "VoterTagAssignments" (
    "VoterId" TEXT NOT NULL,
    "TagId" UUID NOT NULL,
    "AssignedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "AssignedBy" TEXT NOT NULL,
    CONSTRAINT "PK_VoterTagAssignments" PRIMARY KEY ("VoterId", "TagId"),
    CONSTRAINT "FK_VoterTagAssignments_VoterTags_TagId" FOREIGN KEY ("TagId") REFERENCES "VoterTags" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_VoterTagAssignments_Voters_VoterId" FOREIGN KEY ("VoterId") REFERENCES "Voters" ("LalVoterId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "ConsentRecords" (
    "Id" UUID NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "ConsentType" INTEGER NOT NULL,
    "ConsentGiven" BOOLEAN NOT NULL,
    "ConsentDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IpAddress" TEXT,
    "UserAgent" TEXT,
    "ConsentText" TEXT,
    "VoterId" TEXT,
    CONSTRAINT "PK_ConsentRecords" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "VoiceRecordings" (
    "Id" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "FileUrl" TEXT NOT NULL,
    "Duration" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_VoiceRecordings" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ResourceLinks" (
    "Id" UUID NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "Url" TEXT NOT NULL,
    "Icon" VARCHAR(50),
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_ResourceLinks" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TwilioPhoneNumbers" (
    "Id" UUID NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "FriendlyName" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
    "Capabilities" TEXT,
    "AddedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastUsedAt" TIMESTAMP WITH TIME ZONE,
    "TotalCallsMade" INTEGER NOT NULL DEFAULT 0,
    "TotalSmsSent" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "PK_TwilioPhoneNumbers" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TwilioConfiguration" (
    "Id" UUID NOT NULL,
    "Key" TEXT NOT NULL,
    "Value" TEXT NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_TwilioConfiguration" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "PhoneContacts" (
    "Id" UUID NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "FirstName" TEXT,
    "LastName" TEXT,
    "VoterId" TEXT,
    "LastContactedAt" TIMESTAMP WITH TIME ZONE,
    "TotalContacts" INTEGER NOT NULL DEFAULT 0,
    "Notes" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_PhoneContacts" PRIMARY KEY ("Id")
);

-- Create Indexes for Performance
CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "IX_CampaignMessages_CampaignId" ON "CampaignMessages" ("CampaignId");
CREATE INDEX IF NOT EXISTS "IX_CampaignMessages_VoterId" ON "CampaignMessages" ("VoterId");
CREATE INDEX IF NOT EXISTS "IX_Contacts_CampaignId" ON "Contacts" ("CampaignId");
CREATE INDEX IF NOT EXISTS "IX_Contacts_VolunteerId" ON "Contacts" ("VolunteerId");
CREATE INDEX IF NOT EXISTS "IX_Contacts_VoterId" ON "Contacts" ("VoterId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_InvitationTokens_Token" ON "InvitationTokens" ("Token");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TwilioConfiguration_Key" ON "TwilioConfiguration" ("Key");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TwilioPhoneNumbers_PhoneNumber" ON "TwilioPhoneNumbers" ("PhoneNumber");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_VoterTags_Name" ON "VoterTags" ("Name");
CREATE INDEX IF NOT EXISTS "IX_VoterTagAssignments_TagId" ON "VoterTagAssignments" ("TagId");
CREATE INDEX IF NOT EXISTS "IX_Voters_CellPhone" ON "Voters" ("CellPhone");
CREATE INDEX IF NOT EXISTS "IX_Voters_City" ON "Voters" ("City");
CREATE INDEX IF NOT EXISTS "IX_Voters_LastName" ON "Voters" ("LastName");
CREATE INDEX IF NOT EXISTS "IX_Voters_Zip" ON "Voters" ("Zip");
CREATE INDEX IF NOT EXISTS "IX_PhoneContacts_PhoneNumber" ON "PhoneContacts" ("PhoneNumber");

-- Insert initial migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250809000000_ConsolidatedInitialMigration', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Seed initial roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    (gen_random_uuid()::text, 'SuperAdmin', 'SUPERADMIN', gen_random_uuid()::text),
    (gen_random_uuid()::text, 'Admin', 'ADMIN', gen_random_uuid()::text),
    (gen_random_uuid()::text, 'Volunteer', 'VOLUNTEER', gen_random_uuid()::text)
ON CONFLICT DO NOTHING;