-- Migration to add Sealed status to Campaign enum
-- This is needed because the CampaignStatus enum now includes a new value: Sealed = 6

-- The Campaign.Status column already supports integer values,
-- so no schema change is needed. This is just documentation
-- that the new status value 6 = Sealed is now supported.

-- To verify the current campaigns:
SELECT 
    "Id", 
    "Name", 
    "Status",
    CASE "Status"
        WHEN 0 THEN 'Draft'
        WHEN 1 THEN 'Scheduled'
        WHEN 2 THEN 'Sending'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Failed'
        WHEN 5 THEN 'Cancelled'
        WHEN 6 THEN 'Sealed'
        ELSE 'Unknown'
    END as StatusName,
    "SuccessfulDeliveries",
    "FailedDeliveries",
    "PendingDeliveries"
FROM "Campaigns"
ORDER BY "CreatedAt" DESC;