-- CRITICAL FIX: Add Missing User Role Assignments
-- Database: t4h-db (PostgreSQL)
-- Issue: Users have Role column set but missing AspNetUserRoles entries

-- Step 1: Check current state
SELECT 'BEFORE FIX - Current Role Status:' as status;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    u."Role" as "ExpectedRole",
    r."Name" as "AssignedRole",
    CASE 
        WHEN r."Name" = u."Role"::text THEN '✓ CORRECT'
        WHEN r."Name" IS NULL THEN '✗ MISSING ROLE'
        ELSE '⚠ MISMATCH'
    END as "Status"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY u."Role", u."Email";

-- Step 2: Get role IDs for PostgreSQL
DO $$
DECLARE
    volunteer_role_id varchar(450);
    admin_role_id varchar(450);
    superadmin_role_id varchar(450);
    missing_count integer;
BEGIN
    -- Get role IDs
    SELECT "Id" INTO volunteer_role_id FROM "AspNetRoles" WHERE "Name" = 'Volunteer';
    SELECT "Id" INTO admin_role_id FROM "AspNetRoles" WHERE "Name" = 'Admin';
    SELECT "Id" INTO superadmin_role_id FROM "AspNetRoles" WHERE "Name" = 'SuperAdmin';
    
    RAISE NOTICE 'Role IDs Found:';
    RAISE NOTICE 'Volunteer: %', volunteer_role_id;
    RAISE NOTICE 'Admin: %', admin_role_id;
    RAISE NOTICE 'SuperAdmin: %', superadmin_role_id;
    
    -- Insert missing Volunteer roles
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", volunteer_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = volunteer_role_id
    WHERE u."Role" = 1  -- VolunteerRole.Volunteer = 1
      AND ur."UserId" IS NULL
      AND volunteer_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % Volunteer role assignments', missing_count;
    
    -- Insert missing Admin roles
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", admin_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = admin_role_id
    WHERE u."Role" = 2  -- VolunteerRole.Admin = 2
      AND ur."UserId" IS NULL
      AND admin_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % Admin role assignments', missing_count;
    
    -- Insert missing SuperAdmin roles
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", superadmin_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = superadmin_role_id
    WHERE u."Role" = 3  -- VolunteerRole.SuperAdmin = 3
      AND ur."UserId" IS NULL
      AND superadmin_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % SuperAdmin role assignments', missing_count;
END $$;

-- Step 3: Verification
SELECT 'AFTER FIX - Updated Role Status:' as status;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    u."Role" as "ExpectedRole", 
    r."Name" as "AssignedRole",
    CASE 
        WHEN r."Name" = u."Role"::text THEN '✓ CORRECT'
        WHEN r."Name" IS NULL THEN '✗ STILL MISSING'
        ELSE '⚠ MISMATCH'
    END as "Status"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY u."Role", u."Email";

-- Step 4: Summary counts
SELECT 'SUMMARY:' as info;
SELECT 
    'Total Active Users' as "Category",
    COUNT(*) as "Count"
FROM "AspNetUsers"
WHERE "IsActive" = true

UNION ALL

SELECT 
    'Users with Role Assignments' as "Category",
    COUNT(DISTINCT ur."UserId") as "Count"
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true

UNION ALL

SELECT 
    r."Name" || ' Roles Assigned' as "Category",
    COUNT(*) as "Count"
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true
GROUP BY r."Name"
ORDER BY "Category";

-- Step 5: Test email notifications will work
SELECT 'SUPER ADMIN EMAIL CHECK:' as info;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    'Should receive contact notifications' as "EmailStatus"
FROM "AspNetUsers" u
INNER JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE r."Name" = 'SuperAdmin' 
  AND u."IsActive" = true
ORDER BY u."Email";