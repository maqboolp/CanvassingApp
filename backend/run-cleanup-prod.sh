#!/bin/bash

# =====================================================
# PRODUCTION CLEANUP EXECUTION SCRIPT
# This script runs the cleanup on your DigitalOcean database
# =====================================================

echo "=== Production Data Cleanup Script ==="
echo "⚠️  WARNING: This will DELETE all test contact data!"
echo ""
read -p "Have you created a backup? (yes/no): " BACKUP_CONFIRM

if [ "$BACKUP_CONFIRM" != "yes" ]; then
    echo "Please run ./backup-before-cleanup.sh first!"
    exit 1
fi

echo ""
echo "This script will delete:"
echo "  - All contact records"
echo "  - All campaign messages" 
echo "  - Reset all voter contact statuses"
echo "  - Reset campaign tracking counters"
echo ""
read -p "Are you SURE you want to proceed? Type 'DELETE' to confirm: " DELETE_CONFIRM

if [ "$DELETE_CONFIRM" != "DELETE" ]; then
    echo "Cleanup cancelled."
    exit 1
fi

echo ""
echo "Getting database connection info..."

# Get the database URL using doctl
DB_URL=$(doctl databases connection 4210df4e-200d-4397-82d5-c3157127f965 --format URI | grep -v URI)

if [ -z "$DB_URL" ]; then
    echo "Failed to get database connection. Make sure you're logged into doctl."
    exit 1
fi

echo "Connected to database."
echo "Running cleanup script..."

# Create a PostgreSQL-compatible version of the script
cat > /tmp/cleanup-prod.sql << 'EOF'
-- Production cleanup script
BEGIN;

-- 1. Delete all contact records
DELETE FROM "Contacts";

-- 2. Delete all campaign messages
DELETE FROM "CampaignMessages";

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
    "VoterSupport" = NULL;

-- Show summary
SELECT 'Summary after cleanup:' as info;
SELECT 'Contacts' as table_name, COUNT(*) as count FROM "Contacts"
UNION ALL
SELECT 'CampaignMessages', COUNT(*) FROM "CampaignMessages"
UNION ALL
SELECT 'Contacted Voters', COUNT(*) FROM "Voters" WHERE "IsContacted" = true
UNION ALL
SELECT 'Total Voters', COUNT(*) FROM "Voters";

COMMIT;
EOF

# Run the cleanup
psql "$DB_URL" < /tmp/cleanup-prod.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Cleanup completed successfully!"
    echo "All test contact data has been removed."
    echo "The app is ready for production use."
else
    echo ""
    echo "✗ Cleanup failed! Check the error messages above."
    exit 1
fi

# Clean up temp file
rm -f /tmp/cleanup-prod.sql