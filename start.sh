#!/bin/bash

# JLPT Reference Application Startup Script
# This script helps you get started with the Docker Compose setup

echo "🚀 Starting JLPT Reference Application..."
echo "========================================"

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

# Build and start all services
echo "🔨 Building and starting all services..."
docker-compose up --build -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 15

# Run kanji processor to populate database
echo "📚 Populating database with kanji data..."
docker-compose up kanji-processor

# Show service status
echo "📊 Service Status:"
docker-compose ps

echo ""
echo "🎉 JLPT Reference Application is starting up!"
echo "========================================"
echo "🌐 Frontend: http://localhost:3000"
echo "🔧 Backend API: http://localhost:5000"
echo "📚 Swagger API Docs: http://localhost:5000/swagger"
echo "🗄️  PgAdmin: http://localhost:8080"
echo ""
echo "📋 Default PgAdmin credentials:"
echo "   Email: admin@jlptreference.com"
echo "   Password: admin123"
echo ""
echo "🛑 To stop all services: docker-compose down"
echo "📝 To view logs: docker-compose logs -f [service-name]"
echo "🔧 To rebuild: docker-compose up --build"
echo ""
echo "✨ Happy learning Japanese! 🎌"
