-- DIRECT FIX: Add Missing User Role Assignments
-- Simple direct inserts for your specific users

-- Step 1: Show current state
SELECT 'BEFORE - Current assignments:' as status;
SELECT COUNT(*) FROM "AspNetUserRoles";

-- Step 2: Get role IDs
SELECT 'Available roles:' as info;
SELECT "Id", "Name" FROM "AspNetRoles";

-- Step 3: Add missing Volunteer roles (8 users)
-- Use subquery to get Volunteer role ID
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES 
-- Jason Anderson
('cdc18beb-9e22-4760-9b69-c061ad7f989f', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Haniya Halim  
('76453a6f-7199-466d-a520-d534fb933476', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Jonathan Barbie
('30250874-4f11-4a52-9fcf-76e20dd189c1', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Ahmed Ibrahim
('5857ab5c-98d6-410d-81fa-87b32199b954', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Ozair Patel
('203d001d-a125-4299-a62a-5c17b617dbb5', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Adil Patel
('a38c7864-b19b-4493-9dd7-bbf0c994107a', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Tanvir Papa
('59526851-4909-4ef6-9f56-8be2c99c5620', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer')),
-- Arham Anwar
('41a7b993-b0d5-4169-b49f-bfdb97016d68', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Volunteer'))
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

-- Step 4: Add missing Admin roles (2 users)
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES 
-- Rashmee Shariff
('330f11df-5090-419a-bc02-113877971e4b', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Admin')),
-- Britney Garner
('6d979bc8-f3eb-4da3-9791-1130fdf34169', (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'Admin'))
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

-- Step 5: Verification
SELECT 'AFTER - Final verification:' as status;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "FullName",
    COALESCE(r."Name", '❌ NO ROLE') as "RoleAssigned"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY 
    CASE r."Name" 
        WHEN 'SuperAdmin' THEN 1
        WHEN 'Admin' THEN 2  
        WHEN 'Volunteer' THEN 3
        ELSE 4
    END,
    u."Email";

-- Step 6: Summary counts
SELECT 'SUMMARY COUNTS:' as info;
SELECT 
    'Total Active Users' as "Category",
    COUNT(*) as "Count"
FROM "AspNetUsers" WHERE "IsActive" = true

UNION ALL

SELECT 
    'Users with Role Assignments',
    COUNT(DISTINCT ur."UserId")
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true

UNION ALL

SELECT 
    r."Name" || 's',
    COUNT(*)
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true
GROUP BY r."Name"
ORDER BY "Category";

-- Step 7: Email notification test
SELECT 'SuperAdmins for email notifications:' as info;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name"
FROM "AspNetUsers" u
INNER JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE r."Name" = 'SuperAdmin' 
  AND u."IsActive" = true
ORDER BY u."Email";