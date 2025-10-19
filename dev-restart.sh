#!/bin/bash

# Development Restart Script for JLPT Reference Application
# Restarts specific services for development

if [ -z "$1" ]; then
    echo "🔄 Restarting all services..."
    docker-compose restart
else
    echo "🔄 Restarting $1 service..."
    docker-compose restart "$1"
fi

echo "✅ Restart completed!"
