-- Hoover Canvassing App Database Schema
-- PostgreSQL Database Setup

-- Create database (run this separately as a superuser)
-- CREATE DATABASE hoover_canvassing;

-- Create enums
CREATE TYPE vote_frequency AS ENUM ('NonVoter', 'Infrequent', 'Frequent');
CREATE TYPE contact_status AS ENUM ('Reached', 'NotHome', 'Refused', 'NeedsFollowUp');
CREATE TYPE volunteer_role AS ENUM ('Volunteer', 'Admin');

-- AspNetUsers table (Identity framework)
CREATE TABLE "AspNetUsers" (
    "Id" VARCHAR(450) NOT NULL PRIMARY KEY,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    "ConcurrencyStamp" TEXT,
    "Email" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "NormalizedEmail" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "PasswordHash" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "SecurityStamp" TEXT,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "UserName" VARCHAR(256),
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Volunteer',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Voters table
CREATE TABLE "Voters" (
    "LalVoterId" VARCHAR(450) NOT NULL PRIMARY KEY,
    "FirstName" VARCHAR(100) NOT NULL,
    "MiddleName" VARCHAR(100),
    "LastName" VARCHAR(100) NOT NULL,
    "AddressLine" VARCHAR(500) NOT NULL,
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(50) NOT NULL,
    "Zip" VARCHAR(10) NOT NULL,
    "Age" INTEGER NOT NULL,
    "Ethnicity" VARCHAR(100),
    "Gender" VARCHAR(20) NOT NULL,
    "VoteFrequency" VARCHAR(50) NOT NULL,
    "CellPhone" VARCHAR(20),
    "Email" VARCHAR(256),
    "Latitude" DECIMAL(10, 8),
    "Longitude" DECIMAL(11, 8),
    "IsContacted" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastContactStatus" VARCHAR(50)
);

-- Contacts table
CREATE TABLE "Contacts" (
    "Id" VARCHAR(450) NOT NULL PRIMARY KEY,
    "VoterId" VARCHAR(450) NOT NULL,
    "VolunteerId" VARCHAR(450) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Notes" TEXT,
    "Timestamp" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LocationLatitude" DECIMAL(10, 8),
    "LocationLongitude" DECIMAL(11, 8),
    FOREIGN KEY ("VoterId") REFERENCES "Voters"("LalVoterId") ON DELETE RESTRICT,
    FOREIGN KEY ("VolunteerId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT
);

-- VoterAssignments table
CREATE TABLE "VoterAssignments" (
    "VolunteerId" VARCHAR(450) NOT NULL,
    "VoterId" VARCHAR(450) NOT NULL,
    "AssignedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("VolunteerId", "VoterId"),
    FOREIGN KEY ("VolunteerId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("VoterId") REFERENCES "Voters"("LalVoterId") ON DELETE CASCADE
);

-- Additional Identity tables
CREATE TABLE "AspNetRoles" (
    "Id" VARCHAR(450) NOT NULL PRIMARY KEY,
    "ConcurrencyStamp" TEXT,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256)
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" VARCHAR(450) NOT NULL,
    "RoleId" VARCHAR(450) NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    "UserId" VARCHAR(450) NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" VARCHAR(450) NOT NULL,
    "ProviderKey" VARCHAR(450) NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" VARCHAR(450) NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" VARCHAR(450) NOT NULL,
    "LoginProvider" VARCHAR(450) NOT NULL,
    "Name" VARCHAR(450) NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    "RoleId" VARCHAR(450) NOT NULL,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX "IX_Voters_Zip" ON "Voters"("Zip");
CREATE INDEX "IX_Voters_VoteFrequency" ON "Voters"("VoteFrequency");
CREATE INDEX "IX_Voters_IsContacted" ON "Voters"("IsContacted");
CREATE INDEX "IX_Voters_Location" ON "Voters"("Latitude", "Longitude") WHERE "Latitude" IS NOT NULL AND "Longitude" IS NOT NULL;

CREATE INDEX "IX_Contacts_Timestamp" ON "Contacts"("Timestamp");
CREATE INDEX "IX_Contacts_VoterId" ON "Contacts"("VoterId");
CREATE INDEX "IX_Contacts_VolunteerId" ON "Contacts"("VolunteerId");

CREATE INDEX "IX_VoterAssignments_VolunteerId" ON "VoterAssignments"("VolunteerId");
CREATE INDEX "IX_VoterAssignments_VoterId" ON "VoterAssignments"("VoterId");

CREATE INDEX "IX_AspNetUsers_NormalizedEmail" ON "AspNetUsers"("NormalizedEmail");
CREATE INDEX "IX_AspNetUsers_NormalizedUserName" ON "AspNetUsers"("NormalizedUserName");

-- Create spatial index for geolocation queries (if PostGIS is available)
-- CREATE EXTENSION IF NOT EXISTS postgis;
-- ALTER TABLE "Voters" ADD COLUMN "Location" GEOGRAPHY(POINT, 4326);
-- UPDATE "Voters" SET "Location" = ST_MakePoint("Longitude", "Latitude") WHERE "Latitude" IS NOT NULL AND "Longitude" IS NOT NULL;
-- CREATE INDEX "IX_Voters_Location_Spatial" ON "Voters" USING GIST("Location");

-- Insert default admin user (password should be hashed in production)
-- INSERT INTO "AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", 
--                          "EmailConfirmed", "FirstName", "LastName", "Role", "IsActive", "CreatedAt")
-- VALUES ('admin-user-id', 'admin@hoovercanvassing.com', 'ADMIN@HOOVERCANVASSING.COM', 
--         'admin@hoovercanvassing.com', 'ADMIN@HOOVERCANVASSING.COM', 
--         TRUE, 'Admin', 'User', 'Admin', TRUE, CURRENT_TIMESTAMP);

-- Sample data for development (optional)
-- INSERT INTO "Voters" ("LalVoterId", "FirstName", "LastName", "AddressLine", "City", "State", "Zip", 
--                      "Age", "Gender", "VoteFrequency", "CellPhone", "Email")
-- VALUES 
-- ('AL123456789', 'John', 'Smith', '123 Main St', 'Hoover', 'AL', '35226', 45, 'M', 'Frequent', '205-555-0123', 'john.smith@email.com'),
-- ('AL987654321', 'Jane', 'Doe', '456 Oak Ave', 'Hoover', 'AL', '35244', 38, 'F', 'Infrequent', '205-555-0456', 'jane.doe@email.com'),
-- ('AL555666777', 'Bob', 'Johnson', '789 Pine Rd', 'Hoover', 'AL', '35216', 62, 'M', 'Frequent', '205-555-0789', 'bob.johnson@email.com');