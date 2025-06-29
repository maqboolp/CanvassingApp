#!/bin/bash

# =====================================================
# DATABASE BACKUP SCRIPT
# Run this BEFORE executing cleanup-test-data.sql
# =====================================================

echo "=== Database Backup Script ==="
echo "This script will create a backup of your database before cleaning test data"
echo ""

# Get database credentials from environment or prompt
if [ -z "$DATABASE_URL" ]; then
    echo "DATABASE_URL not found in environment."
    echo "Please enter your database connection details:"
    
    read -p "Database host: " DB_HOST
    read -p "Database port (default 5432): " DB_PORT
    DB_PORT=${DB_PORT:-5432}
    read -p "Database name: " DB_NAME
    read -p "Database user: " DB_USER
    read -sp "Database password: " DB_PASS
    echo ""
else
    # Parse DATABASE_URL
    # Format: postgres://user:password@host:port/database
    DB_USER=$(echo $DATABASE_URL | sed -n 's/.*:\/\/\([^:]*\):.*/\1/p')
    DB_PASS=$(echo $DATABASE_URL | sed -n 's/.*:\/\/[^:]*:\([^@]*\)@.*/\1/p')
    DB_HOST=$(echo $DATABASE_URL | sed -n 's/.*@\([^:\/]*\).*/\1/p')
    DB_PORT=$(echo $DATABASE_URL | sed -n 's/.*:\([0-9]*\)\/.*/\1/p')
    DB_NAME=$(echo $DATABASE_URL | sed -n 's/.*\/\([^?]*\).*/\1/p')
fi

# Create backup filename with timestamp
BACKUP_FILE="backup_before_cleanup_$(date +%Y%m%d_%H%M%S).sql"

echo ""
echo "Creating backup: $BACKUP_FILE"
echo "This may take a few minutes..."

# Create the backup
PGPASSWORD=$DB_PASS pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME > $BACKUP_FILE

if [ $? -eq 0 ]; then
    echo "✓ Backup created successfully: $BACKUP_FILE"
    echo ""
    echo "File size: $(ls -lh $BACKUP_FILE | awk '{print $5}')"
    echo ""
    echo "IMPORTANT: Keep this backup file safe!"
    echo "You can now run the cleanup script: cleanup-test-data.sql"
    echo ""
    echo "To restore from this backup later, use:"
    echo "PGPASSWORD=\$DB_PASS psql -h \$DB_HOST -p \$DB_PORT -U \$DB_USER -d \$DB_NAME < $BACKUP_FILE"
else
    echo "✗ Backup failed! Do not proceed with cleanup."
    exit 1
fi