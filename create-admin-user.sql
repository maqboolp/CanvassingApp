-- Create roles if they don't exist
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    ('role-admin-1', 'Admin', 'ADMIN', 'admin-stamp-1'),
    ('role-volunteer-1', 'Volunteer', 'VOLUNTEER', 'volunteer-stamp-1'),
    ('role-superadmin-1', 'SuperAdmin', 'SUPERADMIN', 'superadmin-stamp-1')
ON CONFLICT ("NormalizedName") DO NOTHING;

-- Create admin user
INSERT INTO "AspNetUsers" (
    "Id", 
    "UserName", 
    "NormalizedUserName", 
    "Email", 
    "NormalizedEmail", 
    "EmailConfirmed", 
    "PasswordHash", 
    "SecurityStamp", 
    "ConcurrencyStamp", 
    "PhoneNumberConfirmed", 
    "TwoFactorEnabled", 
    "LockoutEnabled", 
    "AccessFailedCount",
    "FirstName",
    "LastName",
    "Role",
    "IsActive",
    "CreatedAt"
) VALUES (
    'admin-user-1',
    'admin@tanveer4hoover.com',
    'ADMIN@TANVEER4HOOVER.COM',
    'admin@tanveer4hoover.com',
    'ADMIN@TANVEER4HOOVER.COM',
    true,
    'temporary-hash',
    'security-stamp-1',
    'concurrency-stamp-1',
    false,
    false,
    true,
    0,
    'Admin',
    'User',
    2, -- Admin role
    true,
    NOW()
) ON CONFLICT ("NormalizedEmail") DO NOTHING;

-- Assign admin role to the user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT 'admin-user-1', 'role-admin-1'
WHERE EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Id" = 'admin-user-1')
  AND EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "Id" = 'role-admin-1')
  AND NOT EXISTS (SELECT 1 FROM "AspNetUserRoles" WHERE "UserId" = 'admin-user-1' AND "RoleId" = 'role-admin-1');