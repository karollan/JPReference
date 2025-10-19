#!/bin/bash

# JLPT Reference Application - Development Mode Startup Script
# This script starts the application with hot-reload enabled for live coding

echo "🔥 Starting JLPT Reference Application in DEVELOPMENT MODE..."
echo "=============================================================="
echo "🚀 Hot-reload enabled for both frontend and backend!"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Check if environment file exists
if [ ! -f ".env" ]; then
    echo "📝 Creating .env file from template..."
    cp environment.env .env
    echo "✅ Created .env file. You can modify it if needed."
fi

echo "🔨 Building and starting all services with hot-reload..."
echo ""

# Build and start all services
docker-compose up --build -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 15

# Run kanji processor to populate database (only if needed)
echo "📚 Checking if database needs kanji data..."
KANJI_COUNT=$(docker-compose exec -T postgres psql -U jlptuser -d jlptreference -t -c "SELECT COUNT(*) FROM jlpt.kanji;" 2>/dev/null | tr -d ' \n' || echo "0")

if [ "$KANJI_COUNT" = "0" ]; then
    echo "📚 Populating database with kanji data..."
    docker-compose up kanji-processor
else
    echo "✅ Database already has $KANJI_COUNT kanji entries"
fi

# Show service status
echo ""
echo "📊 Service Status:"
docker-compose ps

echo ""
echo "🎉 JLPT Reference Application is running in DEVELOPMENT MODE!"
echo "=============================================================="
echo "🌐 Frontend (Hot-reload): http://localhost:3000"
echo "🔧 Backend API (Hot-reload): http://localhost:5000"
echo "📚 Swagger API Docs: http://localhost:5000/swagger"
echo "🗄️  PgAdmin: http://localhost:8080"
echo ""
echo "🔥 LIVE CODING FEATURES:"
echo "  ✅ Frontend changes will auto-reload in browser"
echo "  ✅ Backend changes will auto-restart the API"
echo "  ✅ No need to rebuild containers for code changes"
echo "  ✅ Database changes persist between restarts"
echo ""
echo "📝 Development Commands:"
echo "  🔍 View logs: docker-compose logs -f [service-name]"
echo "  🛑 Stop services: docker-compose down"
echo "  🔄 Restart service: docker-compose restart [service-name]"
echo "  🧪 Test API: curl http://localhost:5000/api/kanji"
echo ""
echo "📋 Default PgAdmin credentials:"
echo "   Email: admin@jlptreference.com"
echo "   Password: admin123"
echo ""
echo "✨ Happy coding! Changes will appear automatically! 🎌"
