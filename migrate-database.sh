#!/bin/bash

# Script to run database migrations
# This can be run manually or as part of deployment

echo "Running database migrations..."

cd backend/HooverCanvassingApi

# Run Entity Framework migrations
dotnet ef database update

if [ $? -eq 0 ]; then
    echo "Database migrations completed successfully!"
else
    echo "Error: Database migration failed!"
    exit 1
fi