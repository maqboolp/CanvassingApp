-- Fix Missing User Role Assignments for Tanveer for Hoover Campaign
-- This script will assign proper roles to all users based on their Role column in AspNetUsers

-- Step 1: Check current role definitions
SELECT 'Current Roles in AspNetRoles:' as Info;
SELECT Id, Name FROM AspNetRoles ORDER BY Name;

-- Step 2: Check current role assignments  
SELECT 'Current Role Assignments:' as Info;
SELECT COUNT(*) as TotalAssignments FROM AspNetUserRoles;

-- Step 3: Get Role IDs for reference
DECLARE @VolunteerRoleId NVARCHAR(450);
DECLARE @AdminRoleId NVARCHAR(450);  
DECLARE @SuperAdminRoleId NVARCHAR(450);

SELECT @VolunteerRoleId = Id FROM AspNetRoles WHERE Name = 'Volunteer';
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';

SELECT 'Role IDs Found:' as Info;
SELECT 'Volunteer' as Role, @VolunteerRoleId as RoleId
UNION ALL
SELECT 'Admin' as Role, @AdminRoleId as RoleId  
UNION ALL
SELECT 'SuperAdmin' as Role, @SuperAdminRoleId as RoleId;

-- Step 4: Show users missing role assignments
SELECT 'Users Missing Role Assignments:' as Info;
SELECT 
    u.Id as UserId,
    u.Email,
    u.Role as ExpectedRole,
    CASE WHEN ur.UserId IS NULL THEN 'MISSING' ELSE 'HAS ROLE' END as Status
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
ORDER BY u.Role, u.Email;

-- Step 5: Insert missing role assignments (safe for multiple runs)
-- This will only insert if the role assignment doesn't already exist

-- Insert Volunteer role assignments
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT DISTINCT u.Id, @VolunteerRoleId
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId AND ur.RoleId = @VolunteerRoleId
WHERE u.Role = 'Volunteer' 
  AND ur.UserId IS NULL
  AND @VolunteerRoleId IS NOT NULL;

SELECT @@ROWCOUNT as 'Volunteer Roles Added';

-- Insert Admin role assignments  
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT DISTINCT u.Id, @AdminRoleId
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId AND ur.RoleId = @AdminRoleId
WHERE u.Role = 'Admin'
  AND ur.UserId IS NULL
  AND @AdminRoleId IS NOT NULL;

SELECT @@ROWCOUNT as 'Admin Roles Added';

-- Insert SuperAdmin role assignments
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT DISTINCT u.Id, @SuperAdminRoleId
FROM AspNetUsers u  
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId AND ur.RoleId = @SuperAdminRoleId
WHERE u.Role = 'SuperAdmin'
  AND ur.UserId IS NULL
  AND @SuperAdminRoleId IS NOT NULL;

SELECT @@ROWCOUNT as 'SuperAdmin Roles Added';

-- Step 6: Verification - Show final state
SELECT 'Final Verification - All Users Should Have Roles:' as Info;
SELECT 
    u.Email,
    u.Role as UserTableRole,
    r.Name as AssignedRole,
    CASE 
        WHEN u.Role = r.Name THEN '✓ CORRECT'
        WHEN r.Name IS NULL THEN '✗ MISSING ROLE'
        ELSE '⚠ MISMATCH'
    END as Status
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.Role, u.Email;

-- Step 7: Summary counts
SELECT 'Summary:' as Info;
SELECT 
    'Total Users' as Category,
    COUNT(*) as Count
FROM AspNetUsers
WHERE IsActive = 1

UNION ALL

SELECT 
    'Users with Role Assignments' as Category,
    COUNT(DISTINCT ur.UserId) as Count
FROM AspNetUserRoles ur
INNER JOIN AspNetUsers u ON ur.UserId = u.Id
WHERE u.IsActive = 1

UNION ALL

SELECT 
    r.Name + ' Roles Assigned' as Category,
    COUNT(*) as Count
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
INNER JOIN AspNetUsers u ON ur.UserId = u.Id
WHERE u.IsActive = 1
GROUP BY r.Name;