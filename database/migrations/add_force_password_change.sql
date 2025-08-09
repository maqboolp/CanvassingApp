-- Migration: Add ForcePasswordChange column to AspNetUsers table
-- This migration adds forced password change functionality for security

-- Add ForcePasswordChange column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'AspNetUsers' 
        AND column_name = 'ForcePasswordChange'
    ) THEN
        ALTER TABLE "AspNetUsers" 
        ADD COLUMN "ForcePasswordChange" BOOLEAN NOT NULL DEFAULT FALSE;
        
        -- Set force password change for existing admin users
        UPDATE "AspNetUsers" 
        SET "ForcePasswordChange" = TRUE 
        WHERE "Role" IN (1, 2); -- Admin and SuperAdmin roles
        
        RAISE NOTICE 'ForcePasswordChange column added successfully';
    ELSE
        RAISE NOTICE 'ForcePasswordChange column already exists';
    END IF;
END $$;

-- Create default admin users if they don't exist
DO $$
BEGIN
    -- Create SuperAdmin user if not exists
    IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'superadmin@campaign.com') THEN
        INSERT INTO "AspNetUsers" (
            "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
            "FirstName", "LastName", "Role", "IsActive", "CreatedAt", "ForcePasswordChange"
        ) VALUES (
            gen_random_uuid()::text,
            'superadmin@campaign.com',
            'SUPERADMIN@CAMPAIGN.COM',
            'superadmin@campaign.com',
            'SUPERADMIN@CAMPAIGN.COM',
            true,
            -- You'll need to generate proper password hash using .NET
            -- This is a placeholder - replace with actual hash
            'REPLACE_WITH_ACTUAL_HASH',
            gen_random_uuid()::text,
            gen_random_uuid()::text,
            false,
            false,
            true,
            0,
            'Super',
            'Admin',
            2, -- SuperAdmin role
            true,
            CURRENT_TIMESTAMP,
            true -- Force password change on first login
        );
        RAISE NOTICE 'SuperAdmin user created with forced password change';
    END IF;

    -- Create Admin user if not exists
    IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'admin@campaign.com') THEN
        INSERT INTO "AspNetUsers" (
            "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
            "FirstName", "LastName", "Role", "IsActive", "CreatedAt", "ForcePasswordChange"
        ) VALUES (
            gen_random_uuid()::text,
            'admin@campaign.com',
            'ADMIN@CAMPAIGN.COM',
            'admin@campaign.com',
            'ADMIN@CAMPAIGN.COM',
            true,
            -- You'll need to generate proper password hash using .NET
            -- This is a placeholder - replace with actual hash
            'REPLACE_WITH_ACTUAL_HASH',
            gen_random_uuid()::text,
            gen_random_uuid()::text,
            false,
            false,
            true,
            0,
            'Campaign',
            'Admin',
            1, -- Admin role
            true,
            CURRENT_TIMESTAMP,
            true -- Force password change on first login
        );
        RAISE NOTICE 'Admin user created with forced password change';
    END IF;
END $$;

-- Record this migration
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250809001000_AddForcePasswordChange', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;