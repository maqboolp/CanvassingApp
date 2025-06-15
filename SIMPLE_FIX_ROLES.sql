-- SIMPLE FIX: Add Missing User Role Assignments
-- This version works directly with your specific user IDs

-- Step 1: Check what we have
SELECT 'Current role assignments:' as info;
SELECT COUNT(*) as current_assignments FROM "AspNetUserRoles";

SELECT 'Current roles:' as info;
SELECT * FROM "AspNetRoles";

-- Step 2: Insert missing Volunteer roles for the 8 volunteers
-- These are the users from your data that have Role = 'Volunteer' but no role assignments
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."UserId", r."RoleId" FROM (
    VALUES 
    ('cdc18beb-9e22-4760-9b69-c061ad7f989f', 'Volunteer'),
    ('76453a6f-7199-466d-a520-d534fb933476', 'Volunteer'),
    ('30250874-4f11-4a52-9fcf-76e20dd189c1', 'Volunteer'),
    ('5857ab5c-98d6-410d-81fa-87b32199b954', 'Volunteer'),
    ('203d001d-a125-4299-a62a-5c17b617dbb5', 'Volunteer'),
    ('a38c7864-b19b-4493-9dd7-bbf0c994107a', 'Volunteer'),
    ('59526851-4909-4ef6-9f56-8be2c99c5620', 'Volunteer'),
    ('41a7b993-b0d5-4169-b49f-bfdb97016d68', 'Volunteer')
) AS missing(UserId, RoleName)
CROSS JOIN (SELECT "Id" as RoleId FROM "AspNetRoles" WHERE "Name" = missing.RoleName) r
WHERE NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" existing 
    WHERE existing."UserId" = missing.UserId
);

SELECT 'Added volunteer roles for:' as info;
SELECT @@ROWCOUNT as volunteers_added;

-- Step 3: Insert missing Admin roles for the 2 admins  
-- These users have Role = 'Admin' but missing role assignments
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."UserId", r."RoleId" FROM (
    VALUES 
    ('330f11df-5090-419a-bc02-113877971e4b', 'Admin'),
    ('6d979bc8-f3eb-4da3-9791-1130fdf34169', 'Admin')
) AS missing(UserId, RoleName)
CROSS JOIN (SELECT "Id" as RoleId FROM "AspNetRoles" WHERE "Name" = missing.RoleName) r
WHERE NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" existing 
    WHERE existing."UserId" = missing.UserId
);

SELECT 'Added admin roles for:' as info;
SELECT @@ROWCOUNT as admins_added;

-- Step 4: Verification - show all users and their roles
SELECT 'FINAL VERIFICATION:' as info;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    COALESCE(r."Name", 'NO ROLE') as "AssignedRole"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY r."Name", u."Email";

-- Step 5: Count summary
SELECT 'SUMMARY:' as info;
SELECT 
    'Total Users' as category, 
    COUNT(*) as count
FROM "AspNetUsers" WHERE "IsActive" = true
UNION ALL
SELECT 
    'Users with Roles' as category,
    COUNT(DISTINCT ur."UserId") as count  
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true
UNION ALL
SELECT 
    r."Name" || ' Count' as category,
    COUNT(*) as count
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"  
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true
GROUP BY r."Name";

-- Step 6: SuperAdmin email test
SELECT 'SuperAdmins who will get email notifications:' as info;
SELECT u."Email", u."FirstName" || ' ' || u."LastName" as "Name"
FROM "AspNetUsers" u
INNER JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE r."Name" = 'SuperAdmin' AND u."IsActive" = true;