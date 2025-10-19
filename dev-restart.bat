@echo off
REM Development Restart Script for JLPT Reference Application
REM Restarts specific services for development

if "%1"=="" (
    echo 🔄 Restarting all services...
    docker-compose restart
) else (
    echo 🔄 Restarting %1 service...
    docker-compose restart %1
)

echo ✅ Restart completed!
pause
