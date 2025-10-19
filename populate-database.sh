#!/bin/bash

echo "🚀 Populating Database with Kanji Data..."
echo "========================================"

echo "🔄 Running kanji processor..."
docker-compose up kanji-processor

echo ""
echo "📊 Checking results..."
docker-compose exec postgres psql -U jlptuser -d jlptreference -c "SELECT COUNT(*) as total_kanji FROM jlpt.kanji;"

echo ""
echo "✅ Database population completed!"
