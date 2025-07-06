-- Script to add SYSTEM_ADMIN_EMAIL to AppSettings table
-- Run this after applying migrations to set a custom system admin email

-- Example: Set custom system admin email
INSERT INTO "AppSettings" ("Key", "Value", "Description", "Category", "IsPublic", "CreatedAt", "UpdatedAt", "UpdatedBy")
VALUES (
    'SYSTEM_ADMIN_EMAIL',
    'admin@yourcampaign.com', -- Change this to your desired email
    'Email address for the system administrator account. This account cannot be deleted or deactivated.',
    'System',
    false,
    NOW(),
    NOW(),
    'System'
)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value",
    "UpdatedAt" = NOW();

-- Example: Set EMAIL_FROM_ADDRESS for sending emails
INSERT INTO "AppSettings" ("Key", "Value", "Description", "Category", "IsPublic", "CreatedAt", "UpdatedAt", "UpdatedBy")
VALUES (
    'EMAIL_FROM_ADDRESS',
    'noreply@yourcampaign.com', -- Change this to your sending email
    'Email address used as the sender for system notifications',
    'Email',
    false,
    NOW(),
    NOW(),
    'System'
)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value",
    "UpdatedAt" = NOW();