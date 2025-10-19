#!/usr/bin/env python3
"""
Kanji JSON Processor for JLPT Reference Database

This script processes the kanji-jouyou.json file to:
1. Remove fields starting with 'wk_' 
2. Generate SQL insert statements for PostgreSQL
3. Create a clean SQL file for database initialization
"""

import json
import os
import sys
from pathlib import Path

def load_kanji_json(json_file_path):
    """Load and parse the kanji JSON file."""
    try:
        with open(json_file_path, 'r', encoding='utf-8') as file:
            return json.load(file)
    except FileNotFoundError:
        print(f"Error: Could not find {json_file_path}")
        sys.exit(1)
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON: {e}")
        sys.exit(1)

def clean_kanji_data(kanji_data):
    """Remove fields starting with 'wk_' from kanji data."""
    cleaned_data = {}
    
    for character, data in kanji_data.items():
        cleaned_entry = {}
        
        # Keep only non-wk_ fields
        for key, value in data.items():
            if not key.startswith('wk_'):
                cleaned_entry[key] = value
        
        cleaned_data[character] = cleaned_entry
    
    return cleaned_data

def generate_sql_inserts(kanji_data):
    """Generate SQL INSERT statements from cleaned kanji data."""
    sql_statements = []
    
    # Add header comment
    sql_statements.append("-- Kanji data inserts generated from kanji-jouyou.json")
    sql_statements.append("-- Generated automatically - do not edit manually")
    sql_statements.append("")
    
    # Start transaction
    sql_statements.append("BEGIN;")
    sql_statements.append("")
    
    # Generate INSERT statements
    for character, data in kanji_data.items():
        # Extract values with proper formatting
        meanings = data.get('meanings', [])
        readings_on = data.get('readings_on', [])
        readings_kun = data.get('readings_kun', [])
        strokes = data.get('strokes', 0)
        grade = data.get('grade', 0)
        freq = data.get('freq', 0)
        jlpt_old = data.get('jlpt_old', 0)
        jlpt_new = data.get('jlpt_new', 0)
        
        # Format arrays for PostgreSQL
        meanings_str = "ARRAY[" + ",".join([f"'{meaning.replace("'", "''")}'" for meaning in meanings]) + "]"
        readings_on_str = "ARRAY[" + ",".join([f"'{reading.replace("'", "''")}'" for reading in readings_on]) + "]"
        readings_kun_str = "ARRAY[" + ",".join([f"'{reading.replace("'", "''")}'" for reading in readings_kun]) + "]"
        
        # Generate INSERT statement
        insert_sql = f"""INSERT INTO jlpt.kanji (character, meanings, readings_on, readings_kun, stroke_count, grade, frequency, jlpt_old, jlpt_new) VALUES
    ('{character.replace("'", "''")}', {meanings_str}, {readings_on_str}, {readings_kun_str}, {strokes}, {grade}, {freq}, {jlpt_old}, {jlpt_new})
ON CONFLICT (character) DO UPDATE SET
    meanings = EXCLUDED.meanings,
    readings_on = EXCLUDED.readings_on,
    readings_kun = EXCLUDED.readings_kun,
    stroke_count = EXCLUDED.stroke_count,
    grade = EXCLUDED.grade,
    frequency = EXCLUDED.frequency,
    jlpt_old = EXCLUDED.jlpt_old,
    jlpt_new = EXCLUDED.jlpt_new,
    updated_at = CURRENT_TIMESTAMP;"""
        
        sql_statements.append(insert_sql)
    
    # End transaction
    sql_statements.append("")
    sql_statements.append("COMMIT;")
    sql_statements.append("")
    sql_statements.append("-- Kanji data insertion completed")
    
    return sql_statements

def save_cleaned_json(cleaned_data, output_path):
    """Save cleaned kanji data to a new JSON file."""
    with open(output_path, 'w', encoding='utf-8') as file:
        json.dump(cleaned_data, file, ensure_ascii=False, indent=2)

def save_sql_file(sql_statements, output_path):
    """Save SQL statements to a file."""
    with open(output_path, 'w', encoding='utf-8') as file:
        file.write('\n'.join(sql_statements))

def main():
    """Main function to process kanji data."""
    # Get script directory
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    
    # Define paths
    json_input_path = project_root / "source" / "kanji-jouyou.json"
    cleaned_json_path = project_root / "source" / "kanji-jouyou-cleaned.json"
    sql_output_path = project_root / "init" / "02-insert-kanji-data.sql"
    
    print("Processing kanji data...")
    print(f"Input file: {json_input_path}")
    print(f"Cleaned JSON output: {cleaned_json_path}")
    print(f"SQL output: {sql_output_path}")
    
    # Load original data
    print("Loading kanji JSON data...")
    kanji_data = load_kanji_json(json_input_path)
    print(f"Loaded {len(kanji_data)} kanji entries")
    
    # Clean data (remove wk_ fields)
    print("Cleaning data (removing wk_ fields)...")
    cleaned_data = clean_kanji_data(kanji_data)
    
    # Save cleaned JSON
    print("Saving cleaned JSON...")
    save_cleaned_json(cleaned_data, cleaned_json_path)
    
    # Generate SQL inserts
    print("Generating SQL insert statements...")
    sql_statements = generate_sql_inserts(cleaned_data)
    
    # Save SQL file
    print("Saving SQL file...")
    save_sql_file(sql_statements, sql_output_path)
    
    print("Processing completed successfully!")
    print(f"- Cleaned JSON saved to: {cleaned_json_path}")
    print(f"- SQL inserts saved to: {sql_output_path}")
    print(f"- Total kanji entries processed: {len(cleaned_data)}")

if __name__ == "__main__":
    main()
