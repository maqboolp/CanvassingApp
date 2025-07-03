#!/bin/bash

# Manage deployed customer apps
# Usage: ./manage-customer-app.sh <command> <app-id>

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

show_help() {
    echo "Hoover Canvassing App - Customer Management Script"
    echo ""
    echo "Usage: ./manage-customer-app.sh <command> [app-id]"
    echo ""
    echo "Commands:"
    echo "  list                    List all deployed apps"
    echo "  status <app-id>         Get app status and URL"
    echo "  logs <app-id>           View app logs"
    echo "  restart <app-id>        Restart the app"
    echo "  scale <app-id> <size>   Scale app (basic-xxs, basic-xs, basic-s)"
    echo "  backup <app-id>         Backup database"
    echo "  restore <app-id>        Restore database from backup"
    echo "  delete <app-id>         Delete app (with confirmation)"
    echo "  migrate <app-id>        Run database migrations"
    echo "  seed <app-id>           Seed initial admin user"
    echo ""
}

if [ -z "$1" ]; then
    show_help
    exit 1
fi

COMMAND=$1
APP_ID=$2

case $COMMAND in
    "list")
        echo -e "${BLUE}Listing all deployed canvassing apps...${NC}"
        doctl apps list --format "ID,Spec.Name,DefaultIngress,UpdatedAt,ActiveDeployment.Phase" | grep -E "(ID|canvassing-)" || echo "No canvassing apps found"
        ;;
        
    "status")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        echo -e "${BLUE}Getting status for app: ${APP_ID}${NC}"
        doctl apps get ${APP_ID}
        echo -e "\n${BLUE}Database Info:${NC}"
        doctl apps get ${APP_ID} --format "Spec.Databases"
        ;;
        
    "logs")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        echo -e "${BLUE}Viewing logs for app: ${APP_ID}${NC}"
        echo "Press Ctrl+C to stop..."
        doctl apps logs ${APP_ID} --follow
        ;;
        
    "restart")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        echo -e "${YELLOW}Restarting app: ${APP_ID}${NC}"
        DEPLOYMENT_ID=$(doctl apps create-deployment ${APP_ID} --format ID --no-header)
        echo -e "${GREEN}Restart initiated. Deployment ID: ${DEPLOYMENT_ID}${NC}"
        ;;
        
    "scale")
        if [ -z "$APP_ID" ] || [ -z "$3" ]; then
            echo -e "${RED}Error: App ID and size required${NC}"
            echo "Usage: ./manage-customer-app.sh scale <app-id> <size>"
            echo "Sizes: basic-xxs, basic-xs, basic-s, basic-m"
            exit 1
        fi
        SIZE=$3
        echo -e "${BLUE}Scaling app ${APP_ID} to ${SIZE}...${NC}"
        doctl apps update ${APP_ID} --spec - <<EOF
services:
- name: api
  instance_size_slug: ${SIZE}
- name: frontend
  instance_size_slug: ${SIZE}
EOF
        echo -e "${GREEN}Scaling initiated${NC}"
        ;;
        
    "backup")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        echo -e "${BLUE}Creating database backup for app: ${APP_ID}${NC}"
        
        # Get database info
        DB_ID=$(doctl apps get ${APP_ID} --format "Spec.Databases[0].Name" --no-header)
        if [ -z "$DB_ID" ]; then
            echo -e "${RED}No database found for this app${NC}"
            exit 1
        fi
        
        # Create backup using pg_dump
        TIMESTAMP=$(date +%Y%m%d_%H%M%S)
        BACKUP_FILE="${APP_ID}_backup_${TIMESTAMP}.sql"
        
        echo "Creating backup: ${BACKUP_FILE}"
        doctl apps exec ${APP_ID} --command "pg_dump \$DATABASE_URL > /tmp/backup.sql && cat /tmp/backup.sql" > ${BACKUP_FILE}
        
        echo -e "${GREEN}Backup created: ${BACKUP_FILE}${NC}"
        ;;
        
    "delete")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        
        # Get app name for confirmation
        APP_NAME=$(doctl apps get ${APP_ID} --format "Spec.Name" --no-header)
        
        echo -e "${RED}WARNING: This will permanently delete the app and all its data!${NC}"
        echo -e "App to delete: ${APP_NAME} (${APP_ID})"
        read -p "Type the app name to confirm deletion: " CONFIRM_NAME
        
        if [ "$CONFIRM_NAME" != "$APP_NAME" ]; then
            echo -e "${YELLOW}Deletion cancelled${NC}"
            exit 0
        fi
        
        echo -e "${RED}Deleting app...${NC}"
        doctl apps delete ${APP_ID} --force
        echo -e "${GREEN}App deleted successfully${NC}"
        ;;
        
    "migrate")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        echo -e "${BLUE}Running database migrations for app: ${APP_ID}${NC}"
        doctl apps exec ${APP_ID} --command "cd /workspace/out && dotnet HooverCanvassingApi.dll --migrate-only" api
        echo -e "${GREEN}Migrations completed${NC}"
        ;;
        
    "seed")
        if [ -z "$APP_ID" ]; then
            echo -e "${RED}Error: App ID required${NC}"
            exit 1
        fi
        
        echo -e "${BLUE}Creating initial admin user...${NC}"
        read -p "Admin email: " ADMIN_EMAIL
        read -s -p "Admin password: " ADMIN_PASSWORD
        echo
        
        # Create admin user via API or direct database command
        doctl apps exec ${APP_ID} --command "cd /workspace/out && dotnet HooverCanvassingApi.dll --seed-admin --email ${ADMIN_EMAIL} --password ${ADMIN_PASSWORD}" api
        
        echo -e "${GREEN}Admin user created successfully${NC}"
        ;;
        
    *)
        echo -e "${RED}Unknown command: $COMMAND${NC}"
        show_help
        exit 1
        ;;
esac