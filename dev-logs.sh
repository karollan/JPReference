#!/bin/bash

# Development Logs Viewer for JLPT Reference Application
# Shows live logs for all services or specific service

if [ -z "$1" ]; then
    echo "📊 Showing logs for all services..."
    echo "Press Ctrl+C to stop"
    echo "=================================="
    docker-compose logs -f
else
    echo "📊 Showing logs for $1..."
    echo "Press Ctrl+C to stop"
    echo "======================"
    docker-compose logs -f "$1"
fi
