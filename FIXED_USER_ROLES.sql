-- FIXED VERSION: Add Missing User Role Assignments
-- Database: t4h-db (PostgreSQL)
-- Issue: Users have Role column set but missing AspNetUserRoles entries

-- Step 1: Check current state
SELECT 'BEFORE FIX - Current Role Status:' as status;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    CASE u."Role" 
        WHEN 0 THEN 'Volunteer'
        WHEN 1 THEN 'Admin' 
        WHEN 2 THEN 'SuperAdmin'
        ELSE 'Unknown'
    END as "ExpectedRole",
    r."Name" as "AssignedRole",
    CASE 
        WHEN r."Name" IS NOT NULL THEN '✓ HAS ROLE'
        WHEN r."Name" IS NULL THEN '✗ MISSING ROLE'
    END as "Status"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY u."Role", u."Email";

-- Step 2: Show role mappings
SELECT 'Role Mappings:' as info;
SELECT "Id", "Name" FROM "AspNetRoles" ORDER BY "Name";

-- Step 3: Fix missing role assignments
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
    
    -- Insert missing Volunteer roles (Role = 0)
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", volunteer_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = volunteer_role_id
    WHERE u."Role" = 0  -- Volunteer enum value
      AND ur."UserId" IS NULL
      AND volunteer_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % Volunteer role assignments', missing_count;
    
    -- Insert missing Admin roles (Role = 1)
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", admin_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = admin_role_id
    WHERE u."Role" = 1  -- Admin enum value
      AND ur."UserId" IS NULL
      AND admin_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % Admin role assignments', missing_count;
    
    -- Insert missing SuperAdmin roles (Role = 2)
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT DISTINCT u."Id", superadmin_role_id
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId" AND ur."RoleId" = superadmin_role_id
    WHERE u."Role" = 2  -- SuperAdmin enum value
      AND ur."UserId" IS NULL
      AND superadmin_role_id IS NOT NULL
      AND u."IsActive" = true;
    
    GET DIAGNOSTICS missing_count = ROW_COUNT;
    RAISE NOTICE 'Added % SuperAdmin role assignments', missing_count;
END $$;

-- Step 4: Verification
SELECT 'AFTER FIX - Updated Role Status:' as status;
SELECT 
    u."Email",
    u."FirstName" || ' ' || u."LastName" as "Name",
    CASE u."Role" 
        WHEN 0 THEN 'Volunteer'
        WHEN 1 THEN 'Admin' 
        WHEN 2 THEN 'SuperAdmin'
        ELSE 'Unknown'
    END as "ExpectedRole", 
    r."Name" as "AssignedRole",
    CASE 
        WHEN r."Name" IS NOT NULL THEN '✓ CORRECT'
        WHEN r."Name" IS NULL THEN '✗ STILL MISSING'
    END as "Status"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."IsActive" = true
ORDER BY u."Role", u."Email";

-- Step 5: Summary counts
SELECT 'SUMMARY:' as info;
SELECT 
    'Total Active Users' as "Category",
    COUNT(*)::text as "Count"
FROM "AspNetUsers"
WHERE "IsActive" = true

UNION ALL

SELECT 
    'Users with Role Assignments' as "Category",
    COUNT(DISTINCT ur."UserId")::text as "Count"
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true

UNION ALL

SELECT 
    r."Name" || ' Roles Assigned' as "Category",
    COUNT(*)::text as "Count"
FROM "AspNetUserRoles" ur
INNER JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
INNER JOIN "AspNetUsers" u ON ur."UserId" = u."Id"
WHERE u."IsActive" = true
GROUP BY r."Name"
ORDER BY "Category";

-- Step 6: Show specific users that should get email notifications
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