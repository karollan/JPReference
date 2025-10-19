#!/usr/bin/env python3
"""
Kanji Data Processor Runner for Docker Compose

This script is designed to run inside a Docker container to process kanji data
and insert it into the PostgreSQL database after the database is ready.
"""

import os
import sys
import time
import json
import psycopg2
from pathlib import Path

def wait_for_database(max_retries=30, retry_delay=2):
    """Wait for the database to be ready."""
    print("Waiting for database to be ready...")
    
    for attempt in range(max_retries):
        try:
            conn = psycopg2.connect(
                host=os.getenv('POSTGRES_HOST', 'postgres'),
                port=os.getenv('POSTGRES_PORT', '5432'),
                database=os.getenv('POSTGRES_DB', 'jlptreference'),
                user=os.getenv('POSTGRES_USER', 'jlptuser'),
                password=os.getenv('POSTGRES_PASSWORD', 'jlptpassword')
            )
            conn.close()
            print("Database is ready!")
            return True
        except psycopg2.OperationalError as e:
            print(f"Attempt {attempt + 1}/{max_retries}: Database not ready yet - {e}")
            time.sleep(retry_delay)
    
    print("Failed to connect to database after maximum retries")
    return False

def process_and_insert_kanji_data():
    """Process kanji JSON data and insert into database."""
    # Get paths
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    json_file = project_root / "source" / "kanji-jouyou.json"
    
    if not json_file.exists():
        print(f"Error: Kanji JSON file not found at {json_file}")
        return False
    
    # Load kanji data
    print("Loading kanji JSON data...")
    try:
        with open(json_file, 'r', encoding='utf-8') as f:
            kanji_data = json.load(f)
    except Exception as e:
        print(f"Error loading kanji data: {e}")
        return False
    
    print(f"Loaded {len(kanji_data)} kanji entries")
    
    # Connect to database
    try:
        conn = psycopg2.connect(
            host=os.getenv('POSTGRES_HOST', 'postgres'),
            port=os.getenv('POSTGRES_PORT', '5432'),
            database=os.getenv('POSTGRES_DB', 'jlptreference'),
            user=os.getenv('POSTGRES_USER', 'jlptuser'),
            password=os.getenv('POSTGRES_PASSWORD', 'jlptpassword')
        )
        conn.autocommit = False
        cursor = conn.cursor()
    except Exception as e:
        print(f"Error connecting to database: {e}")
        return False
    
    try:
        # Begin transaction
        print("Starting kanji data insertion...")
        
        # Prepare insert statement
        insert_sql = """
            INSERT INTO jlpt.kanji (character, meanings, readings_on, readings_kun, stroke_count, grade, frequency, jlpt_old, jlpt_new)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
            ON CONFLICT (character) DO UPDATE SET
                meanings = EXCLUDED.meanings,
                readings_on = EXCLUDED.readings_on,
                readings_kun = EXCLUDED.readings_kun,
                stroke_count = EXCLUDED.stroke_count,
                grade = EXCLUDED.grade,
                frequency = EXCLUDED.frequency,
                jlpt_old = EXCLUDED.jlpt_old,
                jlpt_new = EXCLUDED.jlpt_new,
                updated_at = CURRENT_TIMESTAMP
        """
        
        # Process and insert data
        inserted_count = 0
        for character, data in kanji_data.items():
            # Clean data (remove wk_ fields)
            meanings = data.get('meanings', [])
            readings_on = data.get('readings_on', [])
            readings_kun = data.get('readings_kun', [])
            strokes = data.get('strokes', 0)
            grade = data.get('grade', 0)
            freq = data.get('freq', 0)
            jlpt_old = data.get('jlpt_old', 0)
            jlpt_new = data.get('jlpt_new', 0)
            
            cursor.execute(insert_sql, (
                character,
                meanings,
                readings_on,
                readings_kun,
                strokes,
                grade,
                freq,
                jlpt_old,
                jlpt_new
            ))
            inserted_count += 1
            
            if inserted_count % 1000 == 0:
                print(f"Processed {inserted_count} kanji entries...")
        
        # Commit transaction
        conn.commit()
        print(f"Successfully inserted/updated {inserted_count} kanji entries")
        
        # Verify insertion
        cursor.execute("SELECT COUNT(*) FROM jlpt.kanji")
        count = cursor.fetchone()[0]
        print(f"Total kanji entries in database: {count}")
        
        return True
        
    except Exception as e:
        print(f"Error during insertion: {e}")
        conn.rollback()
        return False
    finally:
        cursor.close()
        conn.close()

def main():
    """Main function."""
    print("Starting kanji data processor...")
    
    # Wait for database to be ready
    if not wait_for_database():
        sys.exit(1)
    
    # Process and insert kanji data
    if not process_and_insert_kanji_data():
        sys.exit(1)
    
    print("Kanji data processing completed successfully!")

if __name__ == "__main__":
    main()
