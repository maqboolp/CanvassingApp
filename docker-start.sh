#!/bin/bash

# Navigate to deployment directory
cd deployment

# Stop any existing containers
docker-compose down

# Start PostgreSQL and .NET API
echo "Starting PostgreSQL and .NET API..."
docker-compose up -d db api

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 10

# Check if services are running
echo "Checking service status..."
docker-compose ps

echo ""
echo "Services started!"
echo "API is available at: http://localhost:8080"
echo "API Swagger docs: http://localhost:8080/swagger"
echo ""
echo "Default admin credentials:"
echo "Email: admin@tanveer4hoover.com"
echo "Password: Admin123!"
echo ""
echo "To view logs: docker-compose logs -f api"
echo "To stop services: docker-compose down"