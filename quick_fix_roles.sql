-- Quick Fix for Missing User Roles
-- Based on your current data showing role IDs

-- First, let's see what we have:
SELECT 'Current AspNetRoles:' as Info;
SELECT * FROM AspNetRoles;

-- Let me assume the correct role IDs based on your system:
-- You'll need to verify these IDs match your AspNetRoles table

-- Insert missing VOLUNTEER roles (for the 8 volunteers with no role assignments)
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT UserId, RoleId FROM (
    VALUES 
    ('cdc18beb-9e22-4760-9b69-c061ad7f989f', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('76453a6f-7199-466d-a520-d534fb933476', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('30250874-4f11-4a52-9fcf-76e20dd189c1', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('5857ab5c-98d6-410d-81fa-87b32199b954', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('203d001d-a125-4299-a62a-5c17b617dbb5', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('a38c7864-b19b-4493-9dd7-bbf0c994107a', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('59526851-4909-4ef6-9f56-8be2c99c5620', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer')),
    ('41a7b993-b0d5-4169-b49f-bfdb97016d68', (SELECT Id FROM AspNetRoles WHERE Name = 'Volunteer'))
) AS v(UserId, RoleId)
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles ur 
    WHERE ur.UserId = v.UserId AND ur.RoleId = v.RoleId
);

-- Insert missing ADMIN roles (for the 2 admins missing role assignments)  
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT UserId, RoleId FROM (
    VALUES 
    ('330f11df-5090-419a-bc02-113877971e4b', (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')),
    ('6d979bc8-f3eb-4da3-9791-1130fdf34169', (SELECT Id FROM AspNetRoles WHERE Name = 'Admin'))
) AS v(UserId, RoleId)
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles ur 
    WHERE ur.UserId = v.UserId AND ur.RoleId = v.RoleId
);

-- Verification query
SELECT 'Verification - All users should now have roles:' as Info;
SELECT 
    u.Email,
    u.FirstName + ' ' + u.LastName as Name,
    u.Role as ExpectedRole,
    r.Name as AssignedRole,
    CASE 
        WHEN r.Name = u.Role THEN '✓ CORRECT'
        WHEN r.Name IS NULL THEN '✗ STILL MISSING'
        ELSE '⚠ MISMATCH'
    END as Status
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.IsActive = 1
ORDER BY u.Role, u.Email;

-- Count summary
SELECT 
    u.Role,
    COUNT(*) as UsersInRole,
    COUNT(ur.UserId) as UsersWithRoleAssignment
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
WHERE u.IsActive = 1
GROUP BY u.Role
ORDER BY u.Role;