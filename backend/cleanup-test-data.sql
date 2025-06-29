-- =====================================================
-- PRODUCTION DATA CLEANUP SCRIPT
-- This script removes all test contact data
-- Run this ONLY when ready to move to production
-- =====================================================

-- IMPORTANT: Create a backup first!
-- Run this command before executing this script:
-- pg_dump -h your-host -U your-user -d your-database > backup_before_cleanup_$(date +%Y%m%d_%H%M%S).sql

BEGIN TRANSACTION;

-- 1. Delete all contact records
DELETE FROM "Contacts";
PRINT 'Deleted all contact records';

-- 2. Delete all campaign messages
DELETE FROM "CampaignMessages";
PRINT 'Deleted all campaign messages';

-- 3. Reset voter contact-related fields
UPDATE "Voters" 
SET 
    "IsContacted" = false,
    "LastContactStatus" = NULL,
    "LastContactDate" = NULL,
    "TotalCampaignContacts" = 0,
    "SmsCount" = 0,
    "CallCount" = 0,
    "LastSmsDate" = NULL,
    "LastCallDate" = NULL,
    "VoterSupport" = NULL
WHERE 1=1;
PRINT 'Reset all voter contact statuses';

-- 4. Optional: Delete test campaigns (uncomment if needed)
-- DELETE FROM "Campaigns" WHERE "Name" LIKE '%test%' OR "Name" LIKE '%Test%' OR "Name" LIKE '%TEST%';
-- PRINT 'Deleted test campaigns';

-- 5. Reset any SMS consent that was given during testing (optional - uncomment if needed)
-- UPDATE "Voters" 
-- SET 
--     "SmsConsentStatus" = 0,  -- Unknown
--     "SmsOptInDate" = NULL,
--     "SmsOptOutDate" = NULL
-- WHERE "SmsConsentStatus" != 0;
-- PRINT 'Reset SMS consent statuses';

-- 6. Delete consent records from testing (optional - uncomment if needed)
-- DELETE FROM "ConsentRecords";
-- PRINT 'Deleted all consent records';

-- 7. Show summary of data after cleanup
SELECT 'Contacts' as TableName, COUNT(*) as RecordCount FROM "Contacts"
UNION ALL
SELECT 'CampaignMessages', COUNT(*) FROM "CampaignMessages"
UNION ALL
SELECT 'Contacted Voters', COUNT(*) FROM "Voters" WHERE "IsContacted" = true
UNION ALL
SELECT 'Total Voters', COUNT(*) FROM "Voters";

-- Commit the transaction
COMMIT;

PRINT 'Cleanup completed successfully!';
PRINT 'All test contact data has been removed.';
PRINT 'The app is ready for production use.';