@echo off
REM JLPT Reference Application Startup Script for Windows
REM This script helps you get started with the Docker Compose setup

echo 🚀 Starting JLPT Reference Application...
echo ========================================

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not running. Please start Docker Desktop first.
    pause
    exit /b 1
)

REM Check if environment file exists
if not exist ".env" (
    echo 📝 Creating .env file from template...
    copy environment.env .env
    echo ✅ Created .env file. You can modify it if needed.
)

REM Build and start all services
echo 🔨 Building and starting all services...
docker-compose up --build -d

REM Wait for services to be ready
echo ⏳ Waiting for services to start...
timeout /t 15 /nobreak >nul

REM Run kanji processor to populate database
echo 📚 Populating database with kanji data...
docker-compose up kanji-processor

REM Show service status
echo 📊 Service Status:
docker-compose ps

echo.
echo 🎉 JLPT Reference Application is starting up!
echo ========================================
echo 🌐 Frontend: http://localhost:3000
echo 🔧 Backend API: http://localhost:5000
echo 📚 Swagger API Docs: http://localhost:5000/swagger
echo 🗄️  PgAdmin: http://localhost:8080
echo.
echo 📋 Default PgAdmin credentials:
echo    Email: admin@jlptreference.com
echo    Password: admin123
echo.
echo 🛑 To stop all services: docker-compose down
echo 📝 To view logs: docker-compose logs -f [service-name]
echo 🔧 To rebuild: docker-compose up --build
echo.
echo ✨ Happy learning Japanese! 🎌
pause
