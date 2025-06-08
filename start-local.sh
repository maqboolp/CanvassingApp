#!/bin/bash

# Hoover Canvassing App - Local Development Startup Script

set -e

echo "🚀 Starting Tanveer Patel for Hoover City Council Canvassing App locally..."

# Check if PostgreSQL is running
if ! pgrep -x "postgres" > /dev/null; then
    echo "❌ PostgreSQL is not running. Please start PostgreSQL first."
    echo "   macOS: brew services start postgresql"
    echo "   Ubuntu: sudo systemctl start postgresql"
    exit 1
fi

# Check if database exists
if ! psql -lqt | cut -d \| -f 1 | grep -qw hoover_canvassing; then
    echo "📊 Creating database 'hoover_canvassing'..."
    createdb hoover_canvassing
    
    echo "📋 Running database schema..."
    psql -d hoover_canvassing -f database/schema.sql
    
    echo "✅ Database setup complete!"
else
    echo "📊 Database 'hoover_canvassing' already exists"
fi

# Function to start the backend
start_backend() {
    echo "🔧 Starting .NET Core backend..."
    cd backend/HooverCanvassingApi
    
    # Restore packages if needed
    if [ ! -d "bin" ]; then
        echo "📦 Restoring .NET packages..."
        dotnet restore
    fi
    
    # Run migrations if needed
    echo "🔄 Running database migrations..."
    dotnet ef database update 2>/dev/null || echo "⚠️ No migrations to run or EF tools not installed"
    
    echo "🚀 Starting API server on http://localhost:5000"
    dotnet run --urls="http://localhost:5000" &
    BACKEND_PID=$!
    cd ../..
}

# Function to start the frontend
start_frontend() {
    echo "⚛️ Starting React frontend..."
    cd frontend
    
    # Install packages if needed
    if [ ! -d "node_modules" ]; then
        echo "📦 Installing npm packages..."
        npm install
    fi
    
    # Create .env.local if it doesn't exist
    if [ ! -f ".env.local" ]; then
        echo "📝 Creating .env.local..."
        echo "REACT_APP_API_URL=http://localhost:5000" > .env.local
    fi
    
    echo "🚀 Starting React dev server on http://localhost:3000"
    npm start &
    FRONTEND_PID=$!
    cd ..
}

# Function to cleanup on exit
cleanup() {
    echo ""
    echo "🛑 Shutting down services..."
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null || true
    fi
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null || true
    fi
    echo "👋 Goodbye!"
    exit 0
}

# Set up signal handlers
trap cleanup SIGINT SIGTERM

# Start services
start_backend
sleep 3  # Give backend time to start

start_frontend
sleep 3  # Give frontend time to start

echo ""
echo "🎉 Tanveer Patel for Hoover City Council Canvassing App is running!"
echo ""
echo "📱 Frontend: http://localhost:3000"
echo "🔧 Backend API: http://localhost:5000"
echo "📖 API Docs: http://localhost:5000/swagger"
echo "🗄️ Database: hoover_canvassing (PostgreSQL)"
echo ""
echo "Default admin credentials (create these manually):"
echo "   Email: admin@hoovercanvassing.com"
echo "   Password: Admin123!"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Wait for services to run
wait